using LoLProject.ApiService.DTOs;
using LoLProject.Persistence.Models;

namespace LoLProject.ApiService.Services;

// 1. Le Contrat (Interface)
public interface IRiotService
{
    Task<int> SyncChampionsAsync();
    Task<Summoner?> GetSummonerByRiotIdAsync(string gameName, string tagLine);
    Task<DashboardStatsResponseDto?> GetLastMatchesStatsAsync(string puuid);
}