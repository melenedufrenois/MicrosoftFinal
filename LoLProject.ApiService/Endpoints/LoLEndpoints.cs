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
                appUser.Email,
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
                appUser.Email,
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
        group.MapPost("/admin/sync-champions", async (RiotService riotService) =>
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
                champion.IconUrl,
                champion.Description,
                champion.ImageUrl,
                champion.Tips.Select(t => new TipResponseDto(
                    t.Id,
                    t.Content,
                    t.CreatedAt,
                    new AuthorResponseDto(t.Author.Username) // On ne prend que le username
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
        group.MapPost("/dashboard/link", async (LinkSummonerRequest request, AppDb db, RiotService riotService, ClaimsPrincipal user) =>
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
    }
}