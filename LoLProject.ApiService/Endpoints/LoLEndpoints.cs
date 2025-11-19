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

            // 💡 SOLUTION A : On transforme l'entité en DTO sans boucle
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
                .Include(u => u.Summoner) // Important d'include si on veut le renvoyer
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

            // 💡 Transformation en DTO ici aussi
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
        
        // ... (Endpoints Admin inchangés) ...
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
        
        group.MapDelete("/admin/reset-champions", async (AppDb db) => {
            db.Champions.RemoveRange(db.Champions);
            await db.SaveChangesAsync();
            return Results.Ok("Tout est supprimé.");
        });
        
        group.MapGet("/champions/{id:int}", async (int id, AppDb db) =>
        {
            // Attention ici aussi aux boucles si Author a un lien vers ses Tips
            // Pour l'instant je laisse tel quel, mais idéalement il faut un ChampionDetailDto
            var champion = await db.Champions
                .Include(c => c.Tips)
                .ThenInclude(t => t.Author)
                .FirstOrDefaultAsync(c => c.Id == id);

            return champion is not null ? Results.Ok(champion) : Results.NotFound();
        });

        group.MapPost("/champions/{id:int}/tips", async (int id, string content, AppDb db, ClaimsPrincipal user) =>
            {
                var keycloakId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(keycloakId)) return Results.Unauthorized();

                var appUser = await db.AppUsers.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);
                if (appUser == null) return Results.Problem("Utilisateur introuvable.");

                var tip = new ChampionTip
                {
                    ChampionId = id,
                    AppUserId = appUser.Id,
                    Content = content,
                    CreatedAt = DateTime.UtcNow
                };

                db.ChampionTips.Add(tip);
                await db.SaveChangesAsync();
                
                // Pour éviter la boucle sur le retour du Tip (qui contient AppUser qui contient Summoner...), 
                // on peut renvoyer une version anonyme ou un DTO
                return Results.Ok(new { tip.Id, tip.Content, tip.CreatedAt, Author = appUser.Username });
            })
            .RequireAuthorization();
        
        // 6. POST : Lier un compte Riot
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
                    appUser.Summoner.Puuid = summoner.Puuid; // Important de mettre à jour le PUUID aussi
                }
                else
                {
                    appUser.Summoner = summoner;
                }

                await db.SaveChangesAsync();

                // 💡 SOLUTION A : On renvoie le DTO SummonerResponseDto
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