using Content.Shared._ES.Chat.Components;
using Content.Shared.Chat;

namespace Content.Shared._ES.Chat;

public sealed class ESSharedSpeechSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESSpeechChatChannelComponent, ESTransformChatMessageEvent>(OnTransformChatMessage);
        SubscribeLocalEvent<ESSpeechChatChannelComponent, ESTransformMessageSourceNameEvent>(OnTransformSourceName);
    }

    private void OnTransformChatMessage(Entity<ESSpeechChatChannelComponent> ent, ref ESTransformChatMessageEvent args)
    {
        var ev = new TransformSpeechEvent(args.Source, args.Content);
        RaiseLocalEvent(args.Source, ev, true);

        args.Content = ev.Message;
    }

    private void OnTransformSourceName(Entity<ESSpeechChatChannelComponent> ent, ref ESTransformMessageSourceNameEvent args)
    {
        var nameEv = new TransformSpeakerNameEvent(args.Source, args.Name);
        RaiseLocalEvent(args.Source, nameEv);
        args.Name = nameEv.VoiceName;
    }
}
