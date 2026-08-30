using System.Globalization;
using System.Text;
using Content.Shared._ES.Chat.Sanitization.Components;
using Content.Shared.CCVar;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.Speech.Prototypes;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Chat.Sanitization;

public sealed partial class ESSanitizationChatChannelSystem : EntitySystem
{
    [Dependency] private IChatSanitizationManager _sanitizer = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private ESSharedChatSystem _chat = default!;
    [Dependency] private ReplacementAccentSystem _replacementAccent = default!;

    private bool _punctuate;

    private const string TheWordI = "i";

    // Basic textual characters supported in Wormtown9k
    private static readonly HashSet<int> StrictChars = new()
    {
        0x0020, 0x0021, 0x0022, 0x0023, 0x0024, 0x0025, 0x0026, 0x0027, 0x0028, 0x0029, 0x002A, 0x002B, 0x002C, 0x002D,
        0x002E, 0x002F, 0x0030, 0x0031, 0x0032, 0x0033, 0x0034, 0x0035, 0x0036, 0x0037, 0x0038, 0x0039, 0x003A, 0x003B,
        0x037E, 0x003C, 0x003D, 0x003E, 0x003F, 0x0040, 0x0041, 0x0042, 0x0043, 0x0044, 0x0045, 0x0046, 0x0047, 0x0048,
        0x0049, 0x004A, 0x004B, 0x004C, 0x004D, 0x004E, 0x004F, 0x0050, 0x0051, 0x0052, 0x0053, 0x0054, 0x0055, 0x0056,
        0x0057, 0x0058, 0x0059, 0x005A, 0x005B, 0x005C, 0x005D, 0x005E, 0x005F, 0x0060, 0x0061, 0x0062, 0x0063, 0x0064,
        0x0065, 0x0066, 0x0067, 0x0068, 0x0069, 0x006A, 0x006B, 0x006C, 0x006D, 0x006E, 0x006F, 0x0070, 0x0071, 0x0072,
        0x0073, 0x0074, 0x0075, 0x0076, 0x0077, 0x0078, 0x0079, 0x007A, 0x007B, 0x007C, 0x007D, 0x007E, 0x00A0, 0x00A1,
        0x00A2, 0x00A3, 0x00A5, 0x00A6, 0x00A8, 0x00A9, 0x00AB, 0x00AC, 0x00AE, 0x00B0, 0x00B1, 0x00B4, 0x00B5, 0x00B6,
        0x00B7, 0x00B8, 0x00BB, 0x00BF, 0x00C0, 0x00C1, 0x00C2, 0x00C3, 0x00C4, 0x00C5, 0x00C6, 0x00C7, 0x00C8, 0x00C9,
        0x00CA, 0x00CB, 0x00CC, 0x00CD, 0x00CE, 0x00CF, 0x00D0, 0x00D1, 0x00D2, 0x00D3, 0x00D4, 0x00D5, 0x00D6, 0x00D7,
        0x00D8, 0x00D9, 0x00DA, 0x00DB, 0x00DC, 0x00DD, 0x00DE, 0x00DF, 0x00E0, 0x00E1, 0x00E2, 0x00E3, 0x00E4, 0x00E5,
        0x00E6, 0x00E7, 0x00E8, 0x00E9, 0x00EA, 0x00EB, 0x00EC, 0x00ED, 0x00EE, 0x00EF, 0x00F0, 0x00F1, 0x00F2, 0x00F3,
        0x00F4, 0x00F5, 0x00F6, 0x00F7, 0x00F8, 0x00F9, 0x00FA, 0x00FB, 0x00FC, 0x00FD, 0x00FE, 0x00FF, 0x010C, 0x010D,
        0x010E, 0x010F, 0x011A, 0x011B, 0x0131, 0x0147, 0x0148, 0x0152, 0x0153, 0x0158, 0x0159, 0x0160, 0x0161, 0x0164,
        0x0165, 0x016E, 0x016F, 0x0178, 0x017D, 0x017E, 0x0192, 0x2014, 0x2018, 0x2019, 0x201A, 0x201C, 0x201D, 0x201E,
        0x2020, 0x2022, 0x2026, 0x2039, 0x203A, 0x20AC, 0x2122,
    };

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESSanitizationChatChannelComponent, ESPreTransformChatMessageEvent>(OnTransformChatMessage);

        _config.OnValueChanged(CCVars.ChatPunctuation, v => _punctuate = v, true);
    }

    private void OnTransformChatMessage(Entity<ESSanitizationChatChannelComponent> ent, ref ESPreTransformChatMessageEvent args)
    {
        args.Content = SanitizeMessage(args.Source, args.Content, ent);
    }

    private string SanitizeMessage(EntityUid source, string message, Entity<ESSanitizationChatChannelComponent> ent)
    {
        var newMessage = SanitizeMessageReplaceWords(message.Trim());
        newMessage = SanitizeMessageEmojis(newMessage);

        // Sanitize it first as it might change the word order
        if (_sanitizer.TrySanitizeEmoteShorthands(newMessage, source, out newMessage, out var emoteStr))
            _chat.TrySendMessage(emoteStr, ent.Comp.EmoteChannel, source);

        // Capitalizing the word I only happens in English, so we check language here
        var capitalizeTheWordI = !CultureInfo.CurrentCulture.IsNeutralCulture && CultureInfo.CurrentCulture.Parent.Name == "en"
                                        || CultureInfo.CurrentCulture.IsNeutralCulture && CultureInfo.CurrentCulture.Name == "en";

        if (ent.Comp.ShouldCapitalize)
            newMessage = SanitizeMessageCapital(newMessage);
        if (capitalizeTheWordI)
            newMessage = SanitizeMessageCapitalizeTheWordI(newMessage);
        if (_punctuate)
            newMessage = SanitizeMessagePeriod(newMessage);

        return newMessage;
    }

    public static readonly ProtoId<ReplacementAccentPrototype> ChatSanitizeAccent = "chatsanitize";

    public string SanitizeMessageReplaceWords(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        var msg = message;

        msg = _replacementAccent.ApplyReplacements(msg, ChatSanitizeAccent);

        return msg;
    }

    public string SanitizeMessageCapital(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;
        // Capitalize first letter
        message = OopsConcat(char.ToUpper(message[0]).ToString(), message.Remove(0, 1));
        return message;
    }

    // This exists to prevent Roslyn being clever and compiling something that fails sandbox checks.
    private static string OopsConcat(string a, string b) => a + b;

    public string SanitizeMessageCapitalizeTheWordI(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        for
        (
            var index = message.IndexOf(TheWordI, StringComparison.InvariantCulture);
            index != -1;
            index = message.IndexOf(TheWordI, index + 1, StringComparison.InvariantCulture)
        )
        {
            // Stops the code If It's tryIng to capItalIze the letter I In the mIddle of words
            // Repeating the code twice is the simplest option
            if (index + 1 < message.Length && char.IsLetter(message[index + 1]))
                continue;
            if (index - 1 >= 0 && char.IsLetter(message[index - 1]))
                continue;

            var beforeTarget = message.Substring(0, index);
            var target = message.Substring(index, TheWordI.Length);
            var afterTarget = message.Substring(index + TheWordI.Length);

            message = beforeTarget + target.ToUpper() + afterTarget;
        }

        return message;
    }

    private string SanitizeMessagePeriod(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;
        // Adds a period if the last character is a letter.
        if (char.IsLetter(message[^1]))
            message += ".";
        return message;
    }

    private string SanitizeMessageEmojis(string message)
    {
        var sb = new StringBuilder();

        foreach (var rune in message.EnumerateRunes())
        {
            if (StrictChars.Contains(rune.Value))
                sb.Append(rune.ToString());
        }

        return sb.ToString();
    }
}
