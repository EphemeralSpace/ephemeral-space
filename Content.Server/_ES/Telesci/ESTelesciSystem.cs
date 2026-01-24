using Content.Server.Administration;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.Pinpointer;
using Content.Server.Power.EntitySystems;
using Content.Server.RoundEnd;
using Content.Shared._ES.Telesci;
using Content.Shared._ES.Telesci.Components;
using Content.Shared.Administration;
using Robust.Shared.Audio;
using Robust.Shared.Toolshed;
using Robust.Shared.Utility;

namespace Content.Server._ES.Telesci;

public sealed class ESTelesciSystem : ESSharedTelesciSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly NavMapSystem _nav = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESPortalGeneratorComponent, PowerConsumerReceivedChanged>(OnPowerConsumerReceivedChanged);

        // Threats
        SubscribeLocalEvent<ESPortalEventThreatComponent, ComponentStartup>(OnThreatStartup);
        SubscribeLocalEvent<ESPortalEventThreatComponent, EntityTerminatingEvent>(OnThreatTerminating);
    }

    private void OnPowerConsumerReceivedChanged(Entity<ESPortalGeneratorComponent> ent, ref PowerConsumerReceivedChanged args)
    {
        ent.Comp.Powered = args.ReceivedPower >= args.DrawRate;
        Dirty(ent);
    }

    protected override void SpawnEvents(Entity<ESTelesciStationComponent> ent, ESTelesciStage stage)
    {
        base.SpawnEvents(ent, stage);

        foreach (var eventId in EntityTable.GetSpawns(stage.Events))
        {
            _gameTicker.StartGameRule(eventId);
        }
    }

    private void OnThreatStartup(Entity<ESPortalEventThreatComponent> ent, ref ComponentStartup args)
    {
        var query = EntityQueryEnumerator<ESPortalGeneratorComponent>();

        // Update threat count
        while (query.MoveNext(out var uid, out var generator))
        {
            generator.ThreatsLeft += 1;
            Dirty(uid, generator);
        }
    }

    private void OnThreatTerminating(Entity<ESPortalEventThreatComponent> ent, ref EntityTerminatingEvent args)
    {
        var query = EntityQueryEnumerator<ESPortalGeneratorComponent>();

        // Update threat count
        while (query.MoveNext(out var uid, out var generator))
        {
            generator.ThreatsLeft -= 1;
            Dirty(uid, generator);

            // no announcement if not powered
            if (!generator.Powered)
                continue;

            // Make an announcement depending on threats left
            var msg = generator.ThreatsLeft == 0
                ? "es-telesci-threats-none-left-announcement"
                : "es-telesci-threats-some-left-announcement";

            var location = FormattedMessage.RemoveMarkupPermissive(_nav.GetNearestBeaconString(ent.Owner));

            _chat.DispatchStationAnnouncement(uid,
                Loc.GetString(msg, ("location", location), ("threats", generator.ThreatsLeft)),
                Loc.GetString("es-telesci-announcement-sender"),
                announcementSound: new SoundPathSpecifier("/Audio/_ES/Announcements/attention_low.ogg"),
                colorOverride: Color.Magenta);
        }
    }

    protected override void SendAnnouncement(EntityUid ent, ESTelesciStage stage)
    {
        _chat.DispatchStationAnnouncement(ent,
            Loc.GetString(stage.Announcement),
            Loc.GetString("es-telesci-announcement-sender"),
            announcementSound: stage.AnnouncementSound,
            colorOverride: Color.Magenta);
    }

    protected override bool TryCallShuttle(Entity<ESTelesciStationComponent> ent)
    {
        if (!base.TryCallShuttle(ent))
            return false;
        _roundEnd.EndRound();
        return true;
    }
}

[ToolshedCommand, AdminCommand(AdminFlags.Round)]
public sealed class ESTelesciCommand : ToolshedCommand
{
    private ESTelesciSystem? _telesci;

    [CommandImplementation("advanceStage")]
    public void AdvanceStage([PipedArgument] EntityUid station)
    {
        _telesci = Sys<ESTelesciSystem>();
        _telesci.AdvanceTelesciStage(station);
    }

    [CommandImplementation("setStage")]
    public void SetStage([PipedArgument] EntityUid station, int stage)
    {
        _telesci = Sys<ESTelesciSystem>();
        _telesci.SetTelesciStage(station, stage);
    }
}
