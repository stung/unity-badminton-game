/// <summary>
/// What kind of shot a player asks for. The exact shot (clear, drop, smash, ...) is chosen at
/// contact by ShotTable, from the category plus where and how high the birdie is hit.
/// </summary>
public enum ShotCategory
{
    None = 0,
    /// <summary>Cross / J: clear, lift, lift return.</summary>
    Defensive = 1,
    /// <summary>Square / K: drop, net drop, block.</summary>
    Precision = 2,
    /// <summary>Triangle / L: smash, drive.</summary>
    Attack = 3,
}
