using Content.Shared._ES.StatusEffects.Components;
using Content.Shared.Chat;
using Content.Shared.Emoting;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._ES.StatusEffects;

public sealed partial class ESEmoteOnAppliedStatusEffectSystem : EntitySystem
{
    [Dependency] private SharedChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESEmoteOnAppliedStatusEffectComponent, StatusEffectAppliedEvent>(OnStatusEffectApplied);
    }

    private void OnStatusEffectApplied(Entity<ESEmoteOnAppliedStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        _chat.TryEmoteWithChat(args.Target, ent.Comp.Emote, hideLog: true);
    }
}
