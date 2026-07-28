using Content.Server._ES.Announcements;
using Content.Server._ES.Radstorm.Components;
using Content.Server.GameTicking;
using Content.Server.Power.EntitySystems;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.Radstorm.Components;
using Content.Shared.Power;

namespace Content.Server._ES.Radstorm;

public sealed partial class ESRadstormModifierMachineSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private ESAnnouncementSystem _chat = default!;
    [Dependency] private ESEntityTimerSystem _entityTimer = default!;
    [Dependency] private ESRadstormRoundEndRuleSystem _radstormRoundEndRule = default!;
    [Dependency] private GameTicker _ticker = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESRadstormModifierMachineComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<ESRadstormModifierMachineComponent, ESRadstormModifierMachinePowerTimerEvent>(OnPowerTimer);
        SubscribeLocalEvent<ESRadstormModifierMachineComponent, ESThrusterEngineFuelStateChangedEvent>(OnFuelStateChanged);
        SubscribeLocalEvent<GetRadstormSpeedMultiplierEvent>(OnGetMultiplier);
    }

    private void OnPowerChanged(Entity<ESRadstormModifierMachineComponent> ent, ref PowerChangedEvent args)
    {
        if (ent.Comp.TimerEntity.HasValue)
            return;

        // buffer these so that we don't spam the fuck out of them.
        ent.Comp.TimerEntity =
            _entityTimer.SpawnTimer(ent, TimeSpan.FromSeconds(10), new ESRadstormModifierMachinePowerTimerEvent());
    }

    private void OnPowerTimer(Entity<ESRadstormModifierMachineComponent> ent, ref ESRadstormModifierMachinePowerTimerEvent args)
    {
        ent.Comp.TimerEntity = null;
        SetEnabled(ent.AsNullable(), !this.IsPowered(ent.Owner, EntityManager));
    }

    private void OnFuelStateChanged(Entity<ESRadstormModifierMachineComponent> ent, ref ESThrusterEngineFuelStateChangedEvent args)
    {
        SetEnabled(ent.AsNullable(), !args.HasFuel);
    }

    private void OnGetMultiplier(ref GetRadstormSpeedMultiplierEvent ev)
    {
        var query = EntityQueryEnumerator<ESRadstormModifierMachineComponent>();
        while (query.MoveNext(out var comp))
        {
            if (!comp.Enabled)
                continue;

            ev.Speed += comp.Modifier;
        }
    }

    public void SetEnabled(Entity<ESRadstormModifierMachineComponent?> ent, bool value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.Enabled == value)
            return;

        ent.Comp.Enabled = value;
        _appearance.SetData(ent, ESRadstormModifierMachineVisuals.Enabled, value);

        if (!_ticker.IsGameRuleActive<ESRadstormRoundEndRuleComponent>())
            return;

        var newTime = _radstormRoundEndRule.GetRadstormEstimatedArrivalTime();
        var minutes = (int) Math.Round(newTime.TotalMinutes);
        var msg = Loc.GetString(ent.Comp.Enabled ? ent.Comp.EnableAnnouncement : ent.Comp.DisableAnnouncement,
            ("minutes", (minutes)));
        var sound = ent.Comp.Enabled ? ent.Comp.AnnouncementSoundEnabled : ent.Comp.AnnouncementSoundDisabled;
        _chat.DispatchRoundAnnouncement(msg,
            Loc.GetString("es-radstorm-announcer"),
            announcementSound: sound,
            colorOverride: Color.LightSeaGreen,
            important: ent.Comp.Enabled);
        _radstormRoundEndRule.UpdateScreenTimers(Single<ESRadstormRoundEndRuleComponent>(), newTime);
    }
}
