using Content.Shared._ES.Stagehand;
using Content.Shared._ES.Stagehand.Components;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._ES.Stagehand;

/// <see cref="ESStagehandWorldEmoteComponent"/>
public sealed class ESStagehandWorldEmoteSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ESStagehandNotificationsSystem _notif = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    private const float PlayForPlayersInRoundChance = 0.5f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESStagehandWorldEmoteComponent, ESStagehandWorldEmoteEvent>(OnWorldEmoteEvent);
    }

    private void OnWorldEmoteEvent(Entity<ESStagehandWorldEmoteComponent> ent, ref ESStagehandWorldEmoteEvent args)
    {
        if (args.Handled)
            return;

        if (!_proto.TryIndex(args.Emote, out var proto))
            return;

        _notif.SendStagehandNotification(Loc.GetString(proto.Message, ("entity", ent.Owner)));
        var coords = _xform.GetMapCoordinates(ent.Owner);
        var stagehandsInRange = Filter.Empty().AddInRange(coords, 7f).RemoveWhereAttachedEntity(e => !HasComp<ESStagehandComponent>(e));
        var playersInRange = Filter.Empty().AddInRange(coords, 7f).RemoveWhereAttachedEntity(e => HasComp<ESStagehandComponent>(e));

        var resolved = _audio.ResolveSound(proto.Sound);
        _audio.PlayGlobal(resolved, stagehandsInRange, false);

        if (_random.Prob(PlayForPlayersInRoundChance))
        {
            _popup.PopupEntity(Loc.GetString("es-stagehand-emote-performers-heard"), ent, ent, PopupType.SmallCaution);
            _audio.PlayGlobal(resolved, playersInRange, false, proto.Sound.Params.WithVolume(-7f));
        }

        args.Handled = true;
    }
}
