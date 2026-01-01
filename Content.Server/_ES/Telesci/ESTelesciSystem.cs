using System.Linq;
using Content.Server.Administration;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.Power.EntitySystems;
using Content.Server.RoundEnd;
using Content.Shared._ES.Telesci;
using Content.Shared._ES.Telesci.Components;
using Content.Shared.Administration;
using Robust.Server.Audio;
using Robust.Shared.Collections;
using Robust.Shared.Random;
using Robust.Shared.Toolshed;

namespace Content.Server._ES.Telesci;

public sealed class ESTelesciSystem : ESSharedTelesciSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESPortalGeneratorComponent, PowerConsumerReceivedChanged>(OnPowerConsumerReceivedChanged);
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

    protected override void SpawnRewards(Entity<ESTelesciStationComponent> ent, ESTelesciStage stage)
    {
        base.SpawnRewards(ent, stage);

        var rewards = EntityTable.GetSpawns(stage.Rewards).ToList();

        var pads = new ValueList<Entity<ESTelesciRewardPadComponent>>();

        var query = EntityQueryEnumerator<ESTelesciRewardPadComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (!xform.Anchored)
                continue;

            if (comp.Enabled)
                pads.Add((uid, comp));
        }

        var rewardCount = rewards.Count / ent.Comp.RewardPads;
        if (rewardCount <= 0)
            return;

        foreach (var pad in pads)
        {
            for (var i = 0; i < rewardCount; i++)
            {
                if (rewards.Count <= 0)
                    break;
                var item = _random.PickAndTake(rewards);
                SpawnNextToOrDrop(item, pad);
            }
            _audio.PlayPvs(pad.Comp.TeleportSound, pad);
            RaiseNetworkEvent(new ESAnimateTelesciRewardPadMessage(GetNetEntity(pad)));
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
