using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using PokeTeams.Core.Models;

namespace PokeTeams.Core.PokeApi;

/// <summary>
/// Typed HttpClient for https://pokeapi.co/api/v2/. Responses are cached for 24 hours,
/// as PokeAPI's fair-use policy asks.
/// </summary>
public sealed class PokeApiClient(HttpClient http, IMemoryCache cache)
{
    public static readonly Uri BaseAddress = new("https://pokeapi.co/api/v2/");
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    public async Task<IReadOnlyList<PokemonListItem>> ListAsync(int limit = 151, CancellationToken ct = default)
    {
        return await cache.GetOrCreateAsync($"pokeapi:list:{limit}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var page = await http.GetFromJsonAsync<ListDto>($"pokemon?limit={limit}", ct)
                ?? throw new InvalidOperationException("PokeAPI returned an empty list.");
            return (IReadOnlyList<PokemonListItem>)page.Results
                .Select(r => new PokemonListItem(IdFromUrl(r.Url), r.Name))
                .ToList();
        }) ?? [];
    }

    /// <summary>Gets one Pokémon by name or id. Returns null if PokeAPI says 404.</summary>
    public async Task<Pokemon?> GetAsync(string nameOrId, CancellationToken ct = default)
    {
        var key = nameOrId.Trim().ToLowerInvariant();
        return await cache.GetOrCreateAsync($"pokeapi:pokemon:{key}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            using var response = await http.GetAsync($"pokemon/{Uri.EscapeDataString(key)}", ct);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();
            var dto = await response.Content.ReadFromJsonAsync<PokemonDto>(ct)
                ?? throw new InvalidOperationException($"PokeAPI returned no body for '{key}'.");
            return ToModel(dto);
        });
    }

    private static Pokemon ToModel(PokemonDto dto)
    {
        int Stat(string name) => dto.Stats.FirstOrDefault(s => s.Stat.Name == name)?.BaseStat ?? 0;

        return new Pokemon(
            dto.Id,
            dto.Name,
            dto.Sprites.FrontDefault,
            dto.Types.OrderBy(t => t.Slot).Select(t => t.Type.Name).ToList(),
            new BaseStats(
                Stat("hp"), Stat("attack"), Stat("defense"),
                Stat("special-attack"), Stat("special-defense"), Stat("speed")));
    }

    // List URLs look like https://pokeapi.co/api/v2/pokemon/25/
    private static int IdFromUrl(string url) =>
        int.Parse(url.TrimEnd('/').Split('/')[^1], System.Globalization.CultureInfo.InvariantCulture);

    private sealed record ListDto(List<NamedResource> Results);
    private sealed record NamedResource(string Name, string Url);
    private sealed record PokemonDto(int Id, string Name, SpritesDto Sprites, List<TypeSlotDto> Types, List<StatDto> Stats);
    private sealed record SpritesDto([property: JsonPropertyName("front_default")] string? FrontDefault);
    private sealed record TypeSlotDto(int Slot, NamedResource Type);
    private sealed record StatDto([property: JsonPropertyName("base_stat")] int BaseStat, NamedResource Stat);
}
