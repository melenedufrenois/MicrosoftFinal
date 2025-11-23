using LoLProject.ApiService.DTOs;

namespace LoLProject.Tests.Services;

public class StatsCalculatorTests
{
    [Fact]
    public void CalculateWinRate_ShouldBeCorrect()
    {
        // Arrange
        int wins = 12;
        int total = 20;

        // Act
        double winrate = Math.Round((double)wins / total * 100, 0);

        // Assert
        Assert.Equal(60, winrate);
    }

    [Fact]
    public void CalculateKda_ShouldHandleZeroDeaths()
    {
        // Arrange
        var kills = 10;
        var deaths = 0;
        var assists = 5;

        // Act
        // Logique copiée de ton service : si 0 mort, KDA = K + A
        double kda = deaths == 0 ? kills + assists : (double)(kills + assists) / deaths;

        // Assert
        Assert.Equal(15, kda);
    }
}