using System.Collections.Frozen;
using System.Text.RegularExpressions;
using Content.Shared.ActionBlocker;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Content.Shared.Speech;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Chat;

public abstract partial class SharedChatSystem : EntitySystem
{
    public const char RadioCommonPrefix = ';';
    public const char RadioChannelPrefix = ':';
    public const char RadioChannelAltPrefix = '.';
    public const char LocalPrefix = '>';
    public const char ConsolePrefix = '/';
    public const char DeadPrefix = '\\';
    public const char LOOCPrefix = '(';
    public const char OOCPrefix = '[';
    public const char EmotesPrefix = '@';
    public const char EmotesAltPrefix = '*';
    public const char AdminPrefix = ']';
    public const char WhisperPrefix = ',';
    public const char DefaultChannelKey = 'h';

    public const int VoiceRange = 10; // how far voice goes in world units
    public const int WhisperClearRange = 2; // how far whisper goes while still being understandable, in world units
    public const int WhisperMuffledRange = 5; // how far whisper goes at all, in world units

    public static readonly string DefaultChannelPrefix = $"{RadioChannelPrefix}{DefaultChannelKey}";
    public static readonly ProtoId<SpeechVerbPrototype> DefaultSpeechVerb = "Default";

    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypeReload);
        CacheEmotes();
    }

    protected virtual void OnPrototypeReload(PrototypesReloadedEventArgs obj)
    {
        if (obj.WasModified<EmotePrototype>())
            CacheEmotes();
    }

    public static string SanitizeAnnouncement(string message, int maxLength = 0, int maxNewlines = 2)
    {
        var trimmed = message.Trim();
        if (maxLength > 0 && trimmed.Length > maxLength)
        {
            trimmed = $"{message[..maxLength]}...";
        }

        // No more than max newlines, other replaced to spaces
        if (maxNewlines > 0)
        {
            var chars = trimmed.ToCharArray();
            var newlines = 0;
            for (var i = 0; i < chars.Length; i++)
            {
                if (chars[i] != '\n')
                    continue;

                if (newlines >= maxNewlines)
                    chars[i] = ' ';

                newlines++;
            }

            return new string(chars);
        }

        return trimmed;
    }
}

/// <summary>
/// Controls transmission of chat.
/// </summary>
public enum ChatTransmitRange : byte
{
    /// Acts normal, ghosts can hear across the map, etc.
    Normal,
    /// Normal but ghosts are still range-limited.
    GhostRangeLimit,
    /// Hidden from the chat window.
    HideChat,
    /// Ghosts can't hear or see it at all. Regular players can if in-range.
    NoGhosts
}
