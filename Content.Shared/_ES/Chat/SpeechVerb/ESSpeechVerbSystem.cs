using Content.Shared._ES.Chat.SpeechVerb.Components;
using Content.Shared.Chat;
using Robust.Shared.Random;

namespace Content.Shared._ES.Chat.SpeechVerb;

public sealed partial class ESSpeechVerbSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    // TODO: combine this dependency into this system
    [Dependency] private SharedChatSystem _chat = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESSpeechVerbChatChannelComponent, ESGetChatMessageFormatEvent>(OnGetFormat);
    }

    private void OnGetFormat(Entity<ESSpeechVerbChatChannelComponent> ent, ref ESGetChatMessageFormatEvent args)
    {
        var verbPrototype = _chat.GetSpeechVerb(args.Source, args.Content);
        var verb = Loc.GetString(_random.Pick(verbPrototype.SpeechVerbStrings));

        // TODO: pass the rest of the info from SpeechVerbPrototype into the message handling.
        // Right now we only use the verb itself, but there's additional formatting.
        args.Format = Loc.GetString(ent.Comp.Format, ("verb", verb));
    }
}
