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
public sealed class ESCVars : CVars
{
    /// <summary>
    /// What's the current year?
    /// </summary>
    public static readonly CVarDef<int> ESInGameYear =
        CVarDef.Create("es_ic.year", 2066, CVar.SERVER);

    public static readonly CVarDef<bool> ESRandomCharacters =
        CVarDef.Create("es_ic.random_characters", true, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<bool> ESOpenCharacterMenuOnSpawn =
        CVarDef.Create("es_ic.open_character_menu_on_spawn", true, CVar.SERVER | CVar.REPLICATED);

    // RESPAWNING
    public static readonly CVarDef<bool> ESRespawnEnabled =
        CVarDef.Create("es_respawn.enabled", false, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<float> ESRespawnDelay =
        CVarDef.Create("es_respawn.delay", 60f * 10, CVar.SERVER | CVar.REPLICATED);

    // ES-SPECIFIC STATION HANDLING
    public static readonly CVarDef<bool> ESStationEnabled =
        CVarDef.Create("es_station.enabled", true, CVar.SERVER);

    public static readonly CVarDef<string> ESStationCurrentConfig =
        CVarDef.Create("es_station.current_config", "ESDefault", CVar.SERVER);

    // ES-SPECIFIC ARRIVALS HANDLING
    // Used so regular arrivals shit can stay disabled easily
    public static readonly CVarDef<bool> ESArrivalsEnabled =
        CVarDef.Create("es_arrivals.enabled", false, CVar.SERVER);

    // How long in seconds it takes from roundstart->the shuttle arriving at the station
    public static readonly CVarDef<float> ESArrivalsFTLTime =
        CVarDef.Create("es_arrivals.ftl_time", 60 * 4.5f, CVar.SERVER);

    /// <summary>
    ///     Controls whether chat sanitization is enabled for individual users.
    /// </summary>
    public static readonly CVarDef<bool> UserChatSanitizationEnabled =
    CVarDef.Create("es_chat.user_chat_sanitization_enabled", true, CVar.CLIENT | CVar.REPLICATED | CVar.ARCHIVE);

    public static readonly CVarDef<string> FormattedHostName =
        CVarDef.Create("es_status.formatted_host_name", "[{0}] [{1} RolePlay] MyServer", CVar.SERVERONLY);

    public static readonly CVarDef<string> RoleplayLevels =
        CVarDef.Create("es_status.roleplay_levels", "Default", CVar.SERVERONLY);

    /// <summary>
    ///     Controls whether the lobby is "closed".
    ///     While the lobby is closed, the main theater stage is blocked off, the welcome message indicates that the lobby is closed,
    ///     and rounds cannot start.
    /// </summary>
    public static readonly CVarDef<bool> LobbyClosed =
        CVarDef.Create("es_lobby.closed", false, CVar.SERVER | CVar.REPLICATED);

    // Audio occlusion
    public static readonly CVarDef<float> OcclusionMaxOcclusion =
        CVarDef.Create("es_occlusion.max_occlusion", 2f, CVar.SERVER | CVar.REPLICATED);
    public static readonly CVarDef<float> OcclusionVolumeAdjust =
        CVarDef.Create("es_occlusion.volume_adjust", -3f, CVar.SERVER | CVar.REPLICATED);
    public static readonly CVarDef<float> OcclusionMaxDeltaDistance =
        CVarDef.Create("es_occlusion.max_delta_distance", 15f, CVar.SERVER | CVar.REPLICATED);
    public static readonly CVarDef<float> OcclusionMinDeltaDistance =
        CVarDef.Create("es_occlusion.min_delta_distance", 0.5f, CVar.SERVER | CVar.REPLICATED);
}
