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
    public static readonly CVarDef<int> ESInGameYear =
        CVarDef.Create("es_ic.year", 2186, CVar.SERVER);

    public static readonly CVarDef<bool> ESRandomCharacters =
        CVarDef.Create("es_ic.random_characters", true, CVar.SERVER | CVar.REPLICATED);

    // RESPAWNING
    public static readonly CVarDef<bool> ESRespawnEnabled =
        CVarDef.Create("es_respawn.enabled", false, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<float> ESRespawnDelay =
        CVarDef.Create("es_respawn.delay", 60f * 10, CVar.SERVER | CVar.REPLICATED);

    // MULTISTATION
    public static readonly CVarDef<bool> ESMultistationEnabled =
        CVarDef.Create("es_multistation.enabled", false, CVar.SERVER);

    public static readonly CVarDef<string> ESMultistationCurrentConfig =
        CVarDef.Create("es_multistation.current_config", "ESDefault", CVar.SERVER);
}
