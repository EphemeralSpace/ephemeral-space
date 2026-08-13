using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    public static readonly CVarDef<bool> ShowOocPatronColor =
        CVarDef.Create("ooc.show_ooc_patron_color", true, CVar.ARCHIVE | CVar.REPLICATED | CVar.CLIENT);

    /// <summary>
    ///     The discord channel ID to send OOC messages to (also recieve them). This requires the Discord Integration to be enabled and configured.
    /// </summary>
    public static readonly CVarDef<string> OocDiscordChannelId =
        CVarDef.Create("ooc.discord_channel_id", string.Empty, CVar.SERVERONLY);
}
