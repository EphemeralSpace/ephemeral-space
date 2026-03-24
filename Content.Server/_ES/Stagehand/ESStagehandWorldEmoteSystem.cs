using Content.Shared._ES.Stagehand.Components;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._ES.Stagehand;

/// <see cref="ESStagehandWorldEmoteComponent"/>
public sealed class ESStagehandWorldEmoteSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ESStagehandNotificationsSystem _notif = default!;

    private const float PlayForPlayersInRoundChance = 0.25f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESStagehandWorldEmoteComponent, ESStagehandWorldEmoteEvent>(OnWorldEmoteEvent);
    }

    private void OnWorldEmoteEvent(Entity<ESStagehandWorldEmoteComponent> ent, ref ESStagehandWorldEmoteEvent args)
    {
        if (args.Handled)
            return;

        // todo uhh this should definitely just be sending like a prototype id (maybe emoteprototype) instead of the actual message and sound)
        _notif.SendStagehandNotification(Loc.GetString(args.Message, ("entity", ent.Owner)));
        var stagehandsInRange = Filter.Pvs(ent).RemoveWhereAttachedEntity(e => !HasComp<ESStagehandComponent>(e));
        var playersInRange = Filter.Pvs(ent).RemoveWhereAttachedEntity(e => HasComp<ESStagehandComponent>(e));

        _audio.PlayGlobal(args.Sound, stagehandsInRange, false);

        if (_random.Prob(PlayForPlayersInRoundChance))
        {
            _audio.PlayGlobal(args.Sound, playersInRange, false, args.Sound.Params.WithVolume(-12f));
        }

        args.Handled = true;
    }
}
