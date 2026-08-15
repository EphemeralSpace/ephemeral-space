using System.Diagnostics.CodeAnalysis;

namespace Content.Shared._ES.Chat.Sanitization;

public interface IChatSanitizationManager
{
    public void Initialize();

    public bool TrySanitizeEmoteShorthands(string input,
        EntityUid speaker,
        out string sanitized,
        [NotNullWhen(true)] out string? emote);
}
