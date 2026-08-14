using Content.Server.Emoting.Components;
using Content.Shared._ES.Chat;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Hands.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Emoting.Systems;

public sealed partial class BodyEmotesSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private ESEmoteSystem _emote = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyEmotesComponent, EmoteEvent>(OnEmote);
    }

    private void OnEmote(EntityUid uid, BodyEmotesComponent component, ref EmoteEvent args)
    {
        if (args.Handled)
            return;

        var cat = args.Emote.Category;
        if (cat.HasFlag(EmoteCategory.Hands))
        {
            args.Handled = TryEmoteHands(uid, args.Emote, component);
        }
    }

    private bool TryEmoteHands(EntityUid uid, EmotePrototype emote, BodyEmotesComponent component)
    {
        // check that user actually has hands to do emote sound
        if (!TryComp(uid, out HandsComponent? hands) || hands.Count <= 0)
            return false;

        if (!_proto.Resolve(component.SoundsId, out var sounds))
            return false;

        return _emote.TryPlayEmoteSound(uid, sounds, emote);
    }
}
