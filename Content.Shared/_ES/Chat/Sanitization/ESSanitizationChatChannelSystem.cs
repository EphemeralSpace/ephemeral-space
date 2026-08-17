using System.Globalization;
using System.Text.RegularExpressions;
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
    private static readonly Regex EmojiRegex = new(@"\p{Cs}");

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
        return EmojiRegex.Replace(message, string.Empty);
    }
}
