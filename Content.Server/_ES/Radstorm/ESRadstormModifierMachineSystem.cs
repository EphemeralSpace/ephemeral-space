using Content.Server._ES.Radstorm.Components;
using Content.Server.Chat.Systems;
using Content.Shared.Power;

namespace Content.Server._ES.Radstorm;

public sealed class ESRadstormModifierMachineSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly ESRadstormRoundEndRuleSystem _radstormRoundEndRule = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESRadstormModifierMachineComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<GetRadstormSpeedMultiplierEvent>(OnGetMultiplier);
    }

    private void OnPowerChanged(Entity<ESRadstormModifierMachineComponent> ent, ref PowerChangedEvent args)
    {
        SetEnabled(ent.AsNullable(), !args.Powered);
    }

    private void OnGetMultiplier(ref GetRadstormSpeedMultiplierEvent ev)
    {
        var query = EntityQueryEnumerator<ESRadstormModifierMachineComponent>();
        while (query.MoveNext(out var comp))
        {
            if (!comp.Enabled)
                return;

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

        var minutes = (int) Math.Round(_radstormRoundEndRule.GetRadstormEstimatedArrivalTime().TotalMinutes);
        var msg = Loc.GetString(ent.Comp.Enabled ? ent.Comp.EnableAnnouncement : ent.Comp.DisableAnnouncement,
            ("minutes", (minutes)));
        _chat.DispatchGlobalAnnouncement(
            msg,
            Loc.GetString("es-radstorm-announcer"),
            announcementSound: ent.Comp.AnnouncementSound,
            colorOverride: Color.LightSeaGreen);
    }
}
