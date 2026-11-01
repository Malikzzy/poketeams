using PokeTeams.Core.Models;

namespace PokeTeams.Core.Teams;

/// <summary>Pure functions over a set of team members. No I/O.</summary>
public static class TeamAnalyzer
{
    /// <summary>Each stat summed across the team. <see cref="BaseStats.Total"/> is the grand total.</summary>
    public static BaseStats StatTotals(IEnumerable<Pokemon> members)
    {
        var stats = members.Select(m => m.Stats).ToList();
        return new BaseStats(
            stats.Sum(s => s.Hp),
            stats.Sum(s => s.Attack),
            stats.Sum(s => s.Defense),
            stats.Sum(s => s.SpecialAttack),
            stats.Sum(s => s.SpecialDefense),
            stats.Sum(s => s.Speed));
    }

    /// <summary>Average of each stat, keyed by stat name in display order. Empty for an empty team.</summary>
    public static IReadOnlyList<KeyValuePair<string, double>> StatAverages(IEnumerable<Pokemon> members)
    {
        var list = members.ToList();
        if (list.Count == 0)
        {
            return [];
        }
        return StatTotals(list).ToList()
            .Select(s => new KeyValuePair<string, double>(s.Key, (double)s.Value / list.Count))
            .ToList();
    }

    /// <summary>Distinct types on the team, sorted alphabetically.</summary>
    public static IReadOnlyList<string> DistinctTypes(IEnumerable<Pokemon> members) =>
        members.SelectMany(m => m.Types)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.Ordinal)
            .ToList();

    /// <summary>Name of the stat with the lowest team total (first in display order on ties). Null for an empty team.</summary>
    public static string? WeakestStat(IEnumerable<Pokemon> members)
    {
        var list = members.ToList();
        if (list.Count == 0)
        {
            return null;
        }
        return StatTotals(list).ToList().MinBy(s => s.Value).Key;
    }
}
