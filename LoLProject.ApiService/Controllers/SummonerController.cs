using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using LoLProject.Persistence;
using LoLProject.Persistence.Models;
using LoLProject.ApiService.Services;
using System.Security.Claims;

namespace LoLProject.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SummonerController(AppDb db, RiotService riotService) : ControllerBase
{
    // DTOs simples pour l'API
    public record SearchResult(string Puuid, string GameName, string TagLine, int IconId, long Level, bool IsFollowed);
    public record FollowRequest(string Puuid, string GameName, string TagLine, int IconId, long Level);

    [HttpGet("search/{name}/{tag}")]
    public async Task<ActionResult<SearchResult>> Search(string name, string tag)
    {
        // 1. Chercher chez Riot
        var account = await riotService.GetAccountAsync(name, tag);
        if (account == null) return NotFound("Invocateur introuvable chez Riot.");

        var info = await riotService.GetSummonerInfoAsync(account.Value.puuid);
        if (info == null) return NotFound("Info invocateur introuvable.");

        // 2. Vérifier si déjà suivi par l'user (si connecté)
        bool isFollowed = false;
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != null)
        {
             isFollowed = await db.Subscriptions
                .AnyAsync(s => s.AppUser.KeycloakId == userId && s.TrackedSummoner.Puuid == account.Value.puuid);
        }

        return new SearchResult(
            account.Value.puuid, 
            account.Value.gameName, 
            account.Value.tagLine, 
            info.Value.iconId, 
            info.Value.level, 
            isFollowed
        );
    }

    [Authorize]
    [HttpPost("follow")]
    public async Task<IActionResult> Follow([FromBody] FollowRequest req)
    {
        var kId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "no-email";

        // 1. Get or Create User
        var user = await db.AppUsers.FirstOrDefaultAsync(u => u.KeycloakId == kId);
        if (user == null)
        {
            user = new AppUser { KeycloakId = kId!, Email = email };
            db.AppUsers.Add(user);
        }

        // 2. Get or Create Summoner
        var summoner = await db.TrackedSummoners.FirstOrDefaultAsync(s => s.Puuid == req.Puuid);
        if (summoner == null)
        {
            summoner = new TrackedSummoner 
            { 
                Puuid = req.Puuid, 
                GameName = req.GameName, 
                TagLine = req.TagLine, 
                ProfileIconId = req.IconId, 
                SummonerLevel = req.Level 
            };
            db.TrackedSummoners.Add(summoner);
        }

        await db.SaveChangesAsync();

        // 3. Create Subscription if not exists
        if (!await db.Subscriptions.AnyAsync(s => s.AppUserId == user.Id && s.TrackedSummonerId == summoner.Id))
        {
            db.Subscriptions.Add(new Subscription { AppUserId = user.Id, TrackedSummonerId = summoner.Id });
            await db.SaveChangesAsync();
            
            // BACKGROUND: Fetch initial matches
            _ = Task.Run(async () => await RefreshMatchesFor(summoner.Id, summoner.Puuid));
        }

        return Ok();
    }

    [Authorize]
    [HttpGet("favorites")]
    public async Task<ActionResult<List<TrackedSummoner>>> GetFavorites()
    {
        var kId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return await db.Subscriptions
            .Where(s => s.AppUser.KeycloakId == kId)
            .Select(s => s.TrackedSummoner)
            .ToListAsync();
    }

    [Authorize]
    [HttpGet("matches/{puuid}")]
    public async Task<ActionResult<List<MatchCache>>> GetMatches(string puuid)
    {
        // Retourne le cache BDD
        return await db.Matches
            .Where(m => m.TrackedSummoner.Puuid == puuid)
            .OrderByDescending(m => m.GameCreation)
            .ToListAsync();
    }
    
    // Admin endpoint
    [Authorize(Roles = "admin")] 
    [HttpGet("admin/all")]
    public async Task<IActionResult> GetAllSummoners()
    {
        var list = await db.TrackedSummoners
            .Select(s => new {
                s.GameName,
                s.TagLine,
                Followers = s.Subscriptions.Count
            })
            .ToListAsync();
            
        return Ok(list);
    }

    private async Task RefreshMatchesFor(Guid summonerId, string puuid)
    {
        // Hack rapide pour le scope DB dans une tâche background
        using var scope = HttpContext.RequestServices.CreateScope();
        var localDb = scope.ServiceProvider.GetRequiredService<AppDb>(); // On récupère une nouvelle instance du DbContext

        var matches = await riotService.GetLastMatchesAsync(puuid);
        
        foreach(var m in matches)
        {
            // CORRECTION MAJEURE ICI : 
            // On sort la valeur du dynamic AVANT la requête LINQ
            string currentMatchId = (string)m.MatchId; 

            // Maintenant EF Core est content car il compare string == string
            if (!await localDb.Matches.AnyAsync(x => x.RiotMatchId == currentMatchId))
            {
                localDb.Matches.Add(new MatchCache
                {
                    TrackedSummonerId = summonerId,
                    RiotMatchId = currentMatchId,
                    ChampionName = (string)m.Champion,
                    Kills = (int)m.Kills,
                    Deaths = (int)m.Deaths,
                    Assists = (int)m.Assists,
                    Win = (bool)m.Win,
                    GameCreation = (DateTime)m.Date
                });
            }
        }
        await localDb.SaveChangesAsync();
    }
}