using System.Diagnostics.CodeAnalysis;
using Content.Shared._ES.Objectives;
using Content.Shared._ES.Objectives.Components;
using Content.Shared._ES.Telesci.Components;
using Content.Shared.EntityTable;
using Content.Shared.Gravity;
using Content.Shared.Station;
using Robust.Shared.Timing;

namespace Content.Shared._ES.Telesci;

public abstract class ESSharedTelesciSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly EntityTableSystem EntityTable = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly ESSharedObjectiveSystem _objective = default!;
    [Dependency] protected readonly SharedStationSystem Station = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESPortalGeneratorComponent, MapInitEvent>(OnGeneratorMapInit);
        SubscribeLocalEvent<ESPortalGeneratorConsoleComponent, MapInitEvent>(OnConsoleMapInit);
        Subs.BuiEvents<ESPortalGeneratorConsoleComponent>(ESPortalGeneratorConsoleUiKey.Key,
            subs =>
            {
                subs.Event<ESActivePortalGeneratorBuiMessage>(OnActivePortalGenerator);
            }
        );

        SubscribeLocalEvent<ESTelesciObjectiveComponent, ESGetObjectiveProgressEvent>(OnGetObjectiveProgress);
    }

    private void OnGeneratorMapInit(Entity<ESPortalGeneratorComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdateTime = _timing.CurTime + ent.Comp.NextUpdateTime;
        Dirty(ent);
    }

    private void OnConsoleMapInit(Entity<ESPortalGeneratorConsoleComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdateTime = _timing.CurTime + ent.Comp.NextUpdateTime;
    }

    private void OnActivePortalGenerator(Entity<ESPortalGeneratorConsoleComponent> ent, ref ESActivePortalGeneratorBuiMessage args)
    {
        if (!TryGetPortalGenerator(out var generator))
            return;

        if (!generator.Value.Comp.Charged || generator.Value.Comp.ThreatsLeft > 0)
            return;

        if (!Station.TryGetOwningStation<ESTelesciStationComponent>(ent, out var station))
            return;

        AdvanceTelesciStage(station.Value.AsNullable());
    }

    private void OnGetObjectiveProgress(Entity<ESTelesciObjectiveComponent> ent, ref ESGetObjectiveProgressEvent args)
    {
        // Technically we CAN have multiple but this is unsupported behavior.
        foreach (var comp in EntityQuery<ESTelesciStationComponent>())
        {
            if (comp.MaxStage == 0)
                continue;
            args.Progress = (float) comp.Stage / comp.MaxStage;
            break;
        }
    }

    public void AdvanceTelesciStage(Entity<ESTelesciStationComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;
        SetTelesciStage(ent, ent.Comp.Stage + 1);
    }

    public void SetTelesciStage(Entity<ESTelesciStationComponent?> ent, int stageIdx)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.Stage == stageIdx || stageIdx < 0 || stageIdx > ent.Comp.MaxStage)
            return;

        if (TryGetPortalGenerator(out var gen))
            ResetPortalGeneratorProgress(gen.Value);

        var stage = ent.Comp.Stages[stageIdx - 1];

        SpawnEvents((ent, ent.Comp), stage);
        SpawnRewards((ent, ent.Comp), stage);
        SendAnnouncement(ent, stage);

        // TODO: replace with real screen shake once we have it
        foreach (var grid in Station.GetGrids(ent.Owner))
        {
            _gravity.StartGridShake(grid);
        }

        ent.Comp.Stage = stageIdx;
        Dirty(ent);

        _objective.RefreshObjectiveProgress<ESTelesciObjectiveComponent>();

        // End the round after we refresh progress so the EoR screen is always correct.
        TryCallShuttle((ent, ent.Comp));
    }

    protected virtual void SpawnEvents(Entity<ESTelesciStationComponent> ent, ESTelesciStage stage)
    {

    }

    protected virtual void SpawnRewards(Entity<ESTelesciStationComponent> ent, ESTelesciStage stage)
    {

    }

    protected virtual void SendAnnouncement(EntityUid ent, ESTelesciStage stage)
    {

    }

    protected virtual bool TryCallShuttle(Entity<ESTelesciStationComponent> ent)
    {
        return ent.Comp.Stage >= ent.Comp.MaxStage;
    }

    private void UpdateUiState(Entity<ESPortalGeneratorConsoleComponent, UserInterfaceComponent> ent)
    {
        if (Station.GetOwningStation(ent) is not { } station ||
            !TryComp<ESTelesciStationComponent>(station, out var stationComp))
            return;

        if (!TryGetPortalGenerator(out var generator))
            return;

        var state = new ESPortalGeneratorConsoleBuiState
        {
            Charge = (float) Math.Clamp(generator.Value.Comp.AccumulatedChargeTime.TotalSeconds / generator.Value.Comp.ChargeDuration.TotalSeconds, 0, 1),
            Charging = generator.Value.Comp.Powered,
            CurrentResearchStage = stationComp.Stage,
            MaxResearchStage = stationComp.MaxStage,
            ThreatsLeft = generator.Value.Comp.ThreatsLeft
        };
        _userInterface.SetUiState((ent, ent.Comp2), ESPortalGeneratorConsoleUiKey.Key, state);
    }

    public bool TryGetPortalGenerator([NotNullWhen(true)] out Entity<ESPortalGeneratorComponent>? ent)
    {
        var query = EntityQueryEnumerator<ESPortalGeneratorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            ent = (uid, comp);
            return true;
        }

        ent = null;
        return false;
    }

    public void ResetPortalGeneratorProgress(Entity<ESPortalGeneratorComponent> ent)
    {
        ent.Comp.AccumulatedChargeTime = TimeSpan.Zero;
        Dirty(ent);

        _appearance.SetData(ent, ESPortalGeneratorVisuals.Charged, false);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ESPortalGeneratorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.NextUpdateTime)
                continue;
            comp.NextUpdateTime += comp.UpdateDelay;

            if (!comp.Powered)
                continue;
            comp.AccumulatedChargeTime += comp.UpdateDelay;
            Dirty(uid, comp);

            _appearance.SetData(uid, ESPortalGeneratorVisuals.Charged, comp.Charged);
        }

        var consoleQuery = EntityQueryEnumerator<ESPortalGeneratorConsoleComponent, UserInterfaceComponent>();
        while (consoleQuery.MoveNext(out var uid, out var comp, out var ui))
        {
            if (_timing.CurTime < comp.NextUpdateTime)
                continue;
            comp.NextUpdateTime += comp.UpdateDelay;

            UpdateUiState((uid, comp, ui));
        }
    }
}
