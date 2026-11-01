using PokeTeams.Core.Models;

namespace PokeTeams.Core.Teams;

public sealed class Team
{
    public const int MaxSize = 6;

    private readonly List<Pokemon> _members = [];

    public IReadOnlyList<Pokemon> Members => _members;
    public bool IsFull => _members.Count >= MaxSize;

    /// <summary>Adds a member. Fails if the team is full or already has this Pokémon.</summary>
    public bool Add(Pokemon pokemon)
    {
        if (IsFull || Contains(pokemon.Id))
        {
            return false;
        }
        _members.Add(pokemon);
        return true;
    }

    /// <summary>Removes the member with this id. Fails if it isn't on the team.</summary>
    public bool Remove(int pokemonId) => _members.RemoveAll(p => p.Id == pokemonId) > 0;

    public bool Contains(int pokemonId) => _members.Exists(p => p.Id == pokemonId);
}
