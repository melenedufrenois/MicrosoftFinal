using System.Security.Claims;
using LoLProject.ApiService.DTOs;
using LoLProject.Persistence;
using LoLProject.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using LoLProject.ApiService.Services;

namespace LoLProject.ApiService.Endpoints;

public static class LoLEndpoints
{
    public static void MapLoLEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/lol");

        // 1. Endpoint Public
        group.MapGet("/champions", async (AppDb db) =>
        {
            return await db.Champions.ToListAsync();
        });

        // 2. Endpoint Protégé : Récupérer MON Dashboard
        group.MapGet("/dashboard", async (AppDb db, ClaimsPrincipal user) =>
        {
            var keycloakId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(keycloakId)) return Results.Unauthorized();

            var appUser = await db.AppUsers
                .Include(u => u.Summoner)
                .FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

            if (appUser == null)
                return Results.NotFound("Utilisateur non encore enregistré.");

            // 💡 Transformation en DTO pour éviter les boucles JSON
            var response = new AppUserResponseDto(
                appUser.Id,
                appUser.Username,
                appUser.Email ?? "", 
                appUser.Summoner != null 
                    ? new SummonerResponseDto(
                        appUser.Summoner.GameName, 
                        appUser.Summoner.TagLine, 
                        appUser.Summoner.ProfileIconId, 
                        appUser.Summoner.SummonerLevel) 
                    : null
            );

            return Results.Ok(response);
        })
        .RequireAuthorization();

        // 3. Endpoint Protégé : Sync User
        group.MapPost("/sync-user", async (AppDb db, ClaimsPrincipal user) =>
        {
            var keycloakId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = user.FindFirst("preferred_username")?.Value ?? user.Identity?.Name;
            var email = user.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(keycloakId)) return Results.Unauthorized();

            var appUser = await db.AppUsers
                .Include(u => u.Summoner) 
                .FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

            if (appUser == null)
            {
                appUser = new AppUser
                {
                    KeycloakId = keycloakId,
                    Username = username ?? "Unknown",
                    Email = email
                };
                db.AppUsers.Add(appUser);
                await db.SaveChangesAsync();
            }

            var response = new AppUserResponseDto(
                appUser.Id,
                appUser.Username,
                appUser.Email ?? "",
                appUser.Summoner != null 
                    ? new SummonerResponseDto(
                        appUser.Summoner.GameName, 
                        appUser.Summoner.TagLine, 
                        appUser.Summoner.ProfileIconId, 
                        appUser.Summoner.SummonerLevel) 
                    : null
            );

            return Results.Ok(response);
        })
        .RequireAuthorization();
        
        // 4. Endpoint Admin : Sync Champions
        group.MapPost("/admin/sync-champions", async (IRiotService riotService) =>
        {
            try 
            {
                var count = await riotService.SyncChampionsAsync();
                return Results.Ok(new { Message = $"Succès ! {count} champions ajoutés." });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Erreur : {ex.Message}");
            }
        });
        
        // 5. Endpoint Admin : Reset Champions
        group.MapDelete("/admin/reset-champions", async (AppDb db) => {
            db.Champions.RemoveRange(db.Champions);
            await db.SaveChangesAsync();
            return Results.Ok("Tout est supprimé.");
        });
        
        // 6. GET : Détail d'un champion
        group.MapGet("/champions/{id:int}", async (int id, AppDb db) =>
        {
            var champion = await db.Champions
                .Include(c => c.Stats) // On inclut les stats du champion
                .Include(c => c.Tips)
                .ThenInclude(t => t.Author) // On inclut l'auteur pour avoir le pseudo
                .FirstOrDefaultAsync(c => c.Id == id);

            if (champion == null) return Results.NotFound();

            // 💡 Mapping manuel vers DTO pour casser la boucle JSON
            // On convertit les entités en DTOs plats
            var response = new ChampionDetailResponseDto(
                champion.Id,
                champion.Name,
                champion.Title,
                champion.IconUrl ?? "",
                champion.Description ?? "",
                champion.ImageUrl ?? "",
                champion.Tips.Select(t => new TipResponseDto(
                    t.Id,
                    t.Content,
                    t.CreatedAt,
                    new AuthorResponseDto
                    {
                        Id = t.Author.Id,
                        Username = t.Author.Username
                    }
                )).ToList(),
                champion.Stats != null ? new ChampionStatsResponseDto(
                    champion.Stats.Hp, champion.Stats.HpPerLevel,
                    champion.Stats.Mp, champion.Stats.MpPerLevel,
                    champion.Stats.MoveSpeed,
                    champion.Stats.Armor, champion.Stats.ArmorPerLevel,
                    champion.Stats.SpellBlock, champion.Stats.SpellBlockPerLevel,
                    champion.Stats.AttackRange,
                    champion.Stats.AttackDamage, champion.Stats.AttackDamagePerLevel,
                    champion.Stats.AttackSpeed
                ) : null
            );

            return Results.Ok(response);
        });

        // 7. POST : Ajouter un Tip (CORRIGÉ)
        // On utilise 'CreateTipRequest request' au lieu de 'string content' pour lire le Body JSON
        group.MapPost("/champions/{id:int}/tips", async (int id, CreateTipRequest request, AppDb db, ClaimsPrincipal user) =>
            {
                var keycloakId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(keycloakId)) return Results.Unauthorized();

                var appUser = await db.AppUsers.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);
                if (appUser == null) return Results.Problem("Utilisateur introuvable. Avez-vous visité le Dashboard une fois ?");

                var tip = new ChampionTip
                {
                    ChampionId = id,
                    AppUserId = appUser.Id,
                    Content = request.Content, // On récupère le contenu depuis l'objet request
                    CreatedAt = DateTime.UtcNow
                };

                db.ChampionTips.Add(tip);
                await db.SaveChangesAsync();
                
                // On renvoie une réponse simple (anonyme ou DTO) pour confirmer
                return Results.Ok(new { tip.Id, tip.Content, tip.CreatedAt, Author = appUser.Username });
            })
            .RequireAuthorization();
        
        // 8. POST : Lier un compte Riot
        // On utilise 'LinkSummonerRequest request' pour lire le Body JSON
        group.MapPost("/dashboard/link", async (LinkSummonerRequest request, AppDb db, IRiotService riotService, ClaimsPrincipal user) =>
            {
                var keycloakId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(keycloakId)) return Results.Unauthorized();

                var appUser = await db.AppUsers.Include(u => u.Summoner).FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);
                if (appUser == null) return Results.Problem("User local introuvable.");

                var summoner = await riotService.GetSummonerByRiotIdAsync(request.GameName, request.TagLine);
            
                if (summoner == null) 
                    return Results.NotFound("Invocateur introuvable.");

                if (appUser.Summoner != null)
                {
                    appUser.Summoner.SummonerLevel = summoner.SummonerLevel;
                    appUser.Summoner.ProfileIconId = summoner.ProfileIconId;
                    appUser.Summoner.GameName = summoner.GameName;
                    appUser.Summoner.TagLine = summoner.TagLine;
                    appUser.Summoner.Puuid = summoner.Puuid;
                }
                else
                {
                    appUser.Summoner = summoner;
                }

                await db.SaveChangesAsync();

                // 💡 On renvoie le DTO SummonerResponseDto sans boucle
                var response = new SummonerResponseDto(
                    appUser.Summoner.GameName,
                    appUser.Summoner.TagLine,
                    appUser.Summoner.ProfileIconId,
                    appUser.Summoner.SummonerLevel
                );

                return Results.Ok(response);
            })
            .RequireAuthorization();
        
        // 9. GET : Récupérer les stats récentes du joueur lié
        group.MapGet("/dashboard/stats", async (AppDb db, IRiotService riotService, ClaimsPrincipal user) =>
            {
                var keycloakId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(keycloakId)) return Results.Unauthorized();

                // On récupère juste le Summoner lié
                var appUser = await db.AppUsers.Include(u => u.Summoner).FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);
            
                if (appUser?.Summoner == null || string.IsNullOrEmpty(appUser.Summoner.Puuid)) 
                    return Results.BadRequest("Aucun compte Riot lié.");

                // Appel au service Riot
                var stats = await riotService.GetLastMatchesStatsAsync(appUser.Summoner.Puuid);
            
                if (stats == null) return Results.Problem("Impossible de récupérer l'historique.");

                return Results.Ok(stats);
            })
            .RequireAuthorization();
        
        // 10. ADMIN : Récupérer tous les utilisateurs
        group.MapGet("/admin/users", async (AppDb db) =>
        {
            var users = await db.AppUsers
                .Include(u => u.Summoner)
                .ToListAsync();

            // Mapping vers DTO
            var response = users.Select(u => new AppUserResponseDto(
                u.Id,
                u.Username,
                u.Email ?? "",
                u.Summoner != null 
                    ? new SummonerResponseDto(u.Summoner.GameName, u.Summoner.TagLine, u.Summoner.ProfileIconId, u.Summoner.SummonerLevel)
                    : null
            )).ToList();

            return Results.Ok(response);
        })
        .RequireAuthorization(); // Idéalement RequireRole("Admin")
        
        // 11. ADMIN : Délier un utilisateur spécifique
        // 🛑 Changement ici : {userId:guid} et Guid userId
        group.MapDelete("/admin/users/{userId:guid}/unlink", async (Guid userId, AppDb db) =>
            {
                var user = await db.AppUsers.Include(u => u.Summoner).FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null) return Results.NotFound("Utilisateur introuvable");

                user.Summoner = null;
                await db.SaveChangesAsync();

                return Results.Ok("Compte délié avec succès.");
            })
            .RequireAuthorization();

        // 12. ADMIN : Lier un compte
        // 🛑 Changement ici : {userId:guid} et Guid userId
        group.MapPost("/admin/users/{userId:guid}/link", async (Guid userId, LinkSummonerRequest request, AppDb db, IRiotService riotService) =>
            {
                var user = await db.AppUsers.Include(u => u.Summoner).FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null) return Results.NotFound("Utilisateur introuvable");

                var summoner = await riotService.GetSummonerByRiotIdAsync(request.GameName, request.TagLine);
                if (summoner == null) return Results.NotFound("Invocateur Riot introuvable.");

                if (user.Summoner != null)
                {
                    user.Summoner.Puuid = summoner.Puuid;
                    user.Summoner.GameName = summoner.GameName;
                    user.Summoner.TagLine = summoner.TagLine;
                    user.Summoner.ProfileIconId = summoner.ProfileIconId;
                    user.Summoner.SummonerLevel = summoner.SummonerLevel;
                }
                else
                {
                    user.Summoner = summoner;
                }

                await db.SaveChangesAsync();
                return Results.Ok("Compte lié avec succès.");
            })
            .RequireAuthorization();
        
        // 13. ADMIN : Récupérer tous les tips (avec Auteur et Champion)
        group.MapGet("/admin/tips", async (AppDb db) =>
            {
                var tips = await db.ChampionTips
                    .Include(t => t.Author)
                    .Include(t => t.Champion)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                var response = tips.Select(t => new AdminTipDto(
                    t.Id,
                    t.Content,
                    t.CreatedAt,
                    t.Author.Username,
                    t.Champion.Name,
                    t.Champion.IconUrl ?? ""
                ));

                return Results.Ok(response);
            })
            .RequireAuthorization();

        // 14. ADMIN : Supprimer un tip
        group.MapDelete("/admin/tips/{id:int}", async (int id, AppDb db) =>
            {
                var tip = await db.ChampionTips.FindAsync(id);
                if (tip == null) return Results.NotFound();

                db.ChampionTips.Remove(tip);
                await db.SaveChangesAsync();
                return Results.Ok("Tip supprimé.");
            })
            .RequireAuthorization();

        // 15. ADMIN : Modifier un tip
        group.MapPut("/admin/tips/{id:int}", async (int id, UpdateTipRequest request, AppDb db) =>
            {
                var tip = await db.ChampionTips.FindAsync(id);
                if (tip == null) return Results.NotFound();

                tip.Content = request.Content;
                await db.SaveChangesAsync();
                return Results.Ok("Tip modifié.");
            })
            .RequireAuthorization();
        
        // 16. POST : Délier MON compte Riot (Utilisateur connecté)
        group.MapPost("/dashboard/unlink", async (AppDb db, ClaimsPrincipal user) =>
            {
                var keycloakId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(keycloakId)) return Results.Unauthorized();

                var appUser = await db.AppUsers.Include(u => u.Summoner).FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);
                if (appUser == null) return Results.NotFound();

                appUser.Summoner = null; // On supprime la liaison
                await db.SaveChangesAsync();

                return Results.Ok("Compte délié.");
            })
            .RequireAuthorization();
        
        // DELETE : Supprimer son propre tip
        group.MapDelete("/tips/{id:int}", async (int id, AppDb db, ClaimsPrincipal user) =>
            {
                var keycloakId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(keycloakId)) return Results.Unauthorized();

                // On récupère le tip avec son auteur
                var tip = await db.ChampionTips.Include(t => t.Author).FirstOrDefaultAsync(t => t.Id == id);
    
                if (tip == null) return Results.NotFound();

                // VÉRIFICATION DE SÉCURITÉ : Est-ce bien moi l'auteur ?
                if (tip.Author.KeycloakId != keycloakId) 
                    return Results.Forbid(); // 403 Interdit si ce n'est pas mon tip

                db.ChampionTips.Remove(tip);
                await db.SaveChangesAsync();

                return Results.Ok("Tip supprimé.");
            })
            .RequireAuthorization();
    }
}