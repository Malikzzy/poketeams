using PokeTeams.Core.Models;
using PokeTeams.Core.Teams;

namespace PokeTeams.Core.Tests;

public class TeamAnalyzerTests
{
    private static readonly Pokemon[] Starters = [TestData.Bulbasaur, TestData.Charmander, TestData.Pikachu];

    [Fact]
    public void StatTotals_SumsEachStat()
    {
        var totals = TeamAnalyzer.StatTotals(Starters);

        Assert.Equal(45 + 39 + 35, totals.Hp);
        Assert.Equal(45 + 65 + 90, totals.Speed);
        Assert.Equal(318 + 309 + 320, totals.Total);
    }

    [Fact]
    public void StatAverages_DividesByTeamSize()
    {
        var averages = TeamAnalyzer.StatAverages(Starters).ToDictionary(kv => kv.Key, kv => kv.Value);

        Assert.Equal(6, averages.Count);
        Assert.Equal((45 + 39 + 35) / 3.0, averages[BaseStats.HpName], precision: 6);
        Assert.Equal((65 + 60 + 50) / 3.0, averages[BaseStats.SpecialAttackName], precision: 6);
    }

    [Fact]
    public void WeakestStat_IsLowestTotal()
    {
        // HP totals 119, lower than every other stat for these three.
        Assert.Equal(BaseStats.HpName, TeamAnalyzer.WeakestStat(Starters));
    }

    [Fact]
    public void DistinctTypes_AreUniqueAndSorted()
    {
        var team = Starters.Append(TestData.Make(43, "oddish", null, "grass", "poison"));

        Assert.Equal(["electric", "fire", "grass", "poison"], TeamAnalyzer.DistinctTypes(team));
    }

    [Fact]
    public void EmptyTeam_HasNoWeakestStatOrAverages()
    {
        Assert.Null(TeamAnalyzer.WeakestStat([]));
        Assert.Empty(TeamAnalyzer.StatAverages([]));
        Assert.Equal(0, TeamAnalyzer.StatTotals([]).Total);
    }
}
