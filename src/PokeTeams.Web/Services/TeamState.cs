using PokeTeams.Core.Models;
using PokeTeams.Core.Teams;

namespace PokeTeams.Web.Services;

/// <summary>The current user's team. Registered as scoped, so each circuit gets its own.</summary>
public sealed class TeamState
{
    private readonly Team _team = new();

    public event Action? Changed;

    public IReadOnlyList<Pokemon> Members => _team.Members;
    public bool IsFull => _team.IsFull;

    public bool Contains(int pokemonId) => _team.Contains(pokemonId);

    public bool Add(Pokemon pokemon) => Notify(_team.Add(pokemon));

    public bool Remove(int pokemonId) => Notify(_team.Remove(pokemonId));

    private bool Notify(bool changed)
    {
        if (changed)
        {
            Changed?.Invoke();
        }
        return changed;
    }
}
