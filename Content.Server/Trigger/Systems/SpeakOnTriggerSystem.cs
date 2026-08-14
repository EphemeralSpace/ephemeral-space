using Content.Server._ES.Chat;
using Content.Shared._ES.Chat;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Trigger.Systems;

public sealed partial class SpeakOnTriggerSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private ESChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpeakOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<SpeakOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        string message;
        if (ent.Comp.Text != null)
            message = Loc.GetString(ent.Comp.Text);
        else
        {
            if (!_prototypeManager.Resolve(ent.Comp.Pack, out var messagePack))
                return;
            message = Loc.GetString(_random.Pick(messagePack.Values));
        }
        _chat.TrySendMessage(message, ESSharedChatSystem.LocalChannel, target.Value);
        args.Handled = true;
    }
}
