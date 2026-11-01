using PokeTeams.Core.Models;

namespace PokeTeams.Core.Tests;

/// <summary>Hand-built Pokémon so tests never touch the network.</summary>
internal static class TestData
{
    public static Pokemon Make(int id, string name, BaseStats? stats = null, params string[] types) =>
        new(id, name, null, types.Length == 0 ? ["normal"] : types, stats ?? new BaseStats(50, 50, 50, 50, 50, 50));

    public static readonly Pokemon Bulbasaur = new(1, "bulbasaur", null, ["grass", "poison"], new BaseStats(45, 49, 49, 65, 65, 45));
    public static readonly Pokemon Charmander = new(4, "charmander", null, ["fire"], new BaseStats(39, 52, 43, 60, 50, 65));
    public static readonly Pokemon Pikachu = new(25, "pikachu", null, ["electric"], new BaseStats(35, 55, 40, 50, 50, 90));
}
