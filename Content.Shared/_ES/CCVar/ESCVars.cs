using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._ES.CCVar;

/// <summary>
/// Ephemeral Space-specific cvars
/// </summary>
/// <remarks>
/// We won't have such a big cvar list, so we can have it in one file. If it does reach over maybe 200 or so lines, try and separate it into partial classes like upstream
/// </remarks>
[CVarDefs]
// ReSharper disable once InconsistentNaming | shh, be quiet
public sealed partial class ESCVars : CVars
{
    /// <summary>
    /// What's the current year?
    /// </summary>
    public static readonly CVarDef<int> InGameYear =
        CVarDef.Create("ic.year", 2186, CVar.SERVER);

    // RESPAWNING

    public static readonly CVarDef<float> ESRespawnDelay =
        CVarDef.Create("respawn.es_respawn_delay", 60f * 10, CVar.SERVER | CVar.REPLICATED);
}
