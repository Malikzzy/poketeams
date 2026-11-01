namespace PokeTeams.Core.Models;

public sealed record BaseStats(int Hp, int Attack, int Defense, int SpecialAttack, int SpecialDefense, int Speed)
{
    public const string HpName = "HP";
    public const string AttackName = "Attack";
    public const string DefenseName = "Defense";
    public const string SpecialAttackName = "Sp. Atk";
    public const string SpecialDefenseName = "Sp. Def";
    public const string SpeedName = "Speed";

    public int Total => Hp + Attack + Defense + SpecialAttack + SpecialDefense + Speed;

    /// <summary>The six stats in display order, as (name, value) pairs.</summary>
    public IReadOnlyList<KeyValuePair<string, int>> ToList() =>
    [
        new(HpName, Hp),
        new(AttackName, Attack),
        new(DefenseName, Defense),
        new(SpecialAttackName, SpecialAttack),
        new(SpecialDefenseName, SpecialDefense),
        new(SpeedName, Speed),
    ];
}
