namespace PokeTeams.Core.Models;

public sealed record Pokemon(int Id, string Name, string? SpriteUrl, IReadOnlyList<string> Types, BaseStats Stats);

/// <summary>One row of the PokeAPI list endpoint: just enough to show and look up.</summary>
public sealed record PokemonListItem(int Id, string Name);
