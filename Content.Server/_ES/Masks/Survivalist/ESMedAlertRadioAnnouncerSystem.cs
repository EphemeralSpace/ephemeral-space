using Content.Server.Pinpointer;
using Content.Server.Radio.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Robust.Shared.Utility;

namespace Content.Server._ES.Masks.Survivalist;

public sealed class ESMedAlertRadioAnnouncerSystem : EntitySystem
{
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESMedAlertRadioAnnouncerComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<ESMedAlertRadioAnnouncerComponent> ent, ref MobStateChangedEvent args)
    {
        var locId = args.NewMobState switch
        {
            MobState.Critical => ent.Comp.CritMessage,
            MobState.Dead => ent.Comp.DeathMessage,
            _ => null,
        };

        if (locId is null)
            return;

        var location = FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString(ent.Owner));
        var msg = Loc.GetString(locId, ("location", location), ("name", Name(ent.Owner)));
        _radio.SendRadioMessage(ent.Owner, msg, ent.Comp.Channel, ent.Owner);
    }
}
