using Content.Server.Chat.Systems;
using Content.Shared._ES.Radstorm.Components;
using Content.Shared.Power;

namespace Content.Server._ES.Radstorm;

public sealed partial class ESRadstormModifierMachineSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private ESRadstormRoundEndRuleSystem _radstormRoundEndRule = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESRadstormModifierMachineComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<ESRadstormModifierMachineComponent, ESThrusterEngineFuelStateChangedEvent>(OnFuelStateChanged);
        SubscribeLocalEvent<GetRadstormSpeedMultiplierEvent>(OnGetMultiplier);
    }

    private void OnPowerChanged(Entity<ESRadstormModifierMachineComponent> ent, ref PowerChangedEvent args)
    {
        SetEnabled(ent.AsNullable(), !args.Powered);
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

        var minutes = (int) Math.Round(_radstormRoundEndRule.GetRadstormEstimatedArrivalTime().TotalMinutes);
        var msg = Loc.GetString(ent.Comp.Enabled ? ent.Comp.EnableAnnouncement : ent.Comp.DisableAnnouncement,
            ("minutes", (minutes)));
        var sound = ent.Comp.Enabled ? ent.Comp.AnnouncementSoundEnabled : ent.Comp.AnnouncementSoundDisabled;
        _chat.DispatchGlobalAnnouncement(
            msg,
            Loc.GetString("es-radstorm-announcer"),
            announcementSound: sound,
            colorOverride: Color.LightSeaGreen);
    }
}
