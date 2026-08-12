using Content.Server.Actions;
using Content.Server.Administration;
using Content.Server.Radio.EntitySystems;
using Content.Shared._ES.Chat;
using Content.Shared.Actions.Components;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Chat.Transponder;

/// <summary>
///     Sends a message to a radio channel, outside of the regular chat paradigm, using an action.
/// </summary>
public sealed partial class ESTransponderSystem : EntitySystem
{
    [Dependency] private RadioSystem _radio = default!;
    [Dependency] private QuickDialogSystem _dialog = default!;
    [Dependency] private ActionsSystem _action = default!;

    public override void Initialize()
    {
        base.Initialize();

        // only works on entities that can intrinsically receive radio (hack) (i will replcae later this is prototyping hours)
        //SubscribeLocalEvent<IntrinsicRadioReceiverComponent, ESTransponderActionEvent>(OnUseTransponder);
    }

    /*
    private void OnUseTransponder(Entity<IntrinsicRadioReceiverComponent> ent, ref ESTransponderActionEvent args)
    {
        // TODO: reimplement with a custom chat channel
        /*
        if (!TryComp<ActiveRadioComponent>(ent, out var radio) ||
            (!radio.Channels.Contains(args.Channel) && !radio.ReceiveAllChannels))
            return;

        if (!TryComp<ActorComponent>(ent, out var actor))
            return;

        var channel = args.Channel;
        var action = args.Action;
        _dialog.OpenDialog<string>(actor.PlayerSession,
            Loc.GetString("es-transponder-dialog-title"),
            Loc.GetString("es-transponder-dialog-prompt"),
            (msg => SendMessage(ent, channel, msg, action)));

        // we deliberately do not set handled, because we activate the usedelay when a message is actually sent
    }
    */

    private void SendMessage(EntityUid ent, ProtoId<RadioChannelPrototype> channel, string message, Entity<ActionComponent> action)
    {
        if (_action.IsCooldownActive(action))
            return;

        _radio.SendRadioMessage(ent, message, channel, ent, force: true);
        _action.StartUseDelay(action.AsNullable());
    }
}
