using System.Linq;
using Content.Client._ES.Core;
using Content.Client._ES.Masks;
using Content.Client._ES.Objectives;
using Content.Client._ES.Objectives.Ui;
using Content.Client.Gameplay;
using Content.Client.Roles;
using Content.Shared._ES.Auditions.Components;
using Content.Shared._ES.Masks;
using Content.Shared._ES.Objectives.Components;
using Content.Shared._ES.Stagehand.Components;
using Content.Shared.Mind;
using Content.Shared.Warps;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._ES.Stagehand.Ui;

[UsedImplicitly]
public sealed class StagehandObserveUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [UISystemDependency] private readonly JobSystem _job = default!;
    [UISystemDependency] private readonly ESMaskSystem? _mask = default!;
    [UISystemDependency] private readonly ESObjectiveSystem? _objective = default!;

    public void OnStateEntered(GameplayState state)
    {
        _mask?.OnMaskChanged += OnMaskChanged;
        _objective?.OnObjectivesChanged += OnObjectivesChanged;

        if (UIManager.GetActiveUIWidgetOrNull<ESStagehandObserveControl>() is not { } observe)
            return;

        Update(observe);
    }

    public void OnStateExited(GameplayState state)
    {
        _mask?.OnMaskChanged -= OnMaskChanged;
        _objective?.OnObjectivesChanged -= OnObjectivesChanged;
    }

    private void OnMaskChanged(EntityUid mind, ProtoId<ESMaskPrototype>? mask)
    {
        if (UIManager.GetActiveUIWidgetOrNull<ESStagehandObserveControl>() is not { } observe)
            return;

        Update(observe);
        UpdateInfoPanel(observe);
    }

    private void OnObjectivesChanged(Entity<ESObjectiveHolderComponent> mind)
    {
        if (UIManager.GetActiveUIWidgetOrNull<ESStagehandObserveControl>() is not { } observe)
            return;

        if (mind == observe.CurrentEntity)
            UpdateInfoPanel(observe);
    }

    public void Update(ESStagehandObserveControl observe)
    {
        observe.MindsContainer.Children.Clear();

        var minds = new List<Entity<MindComponent, ESCharacterComponent>>();

        var query = EntityManager.EntityQueryEnumerator<ESCharacterComponent, MindComponent>();
        while (query.MoveNext(out var uid, out var character, out var mind))
        {
            if (mind.OriginalOwnerUserId is null)
                continue;

            minds.Add((uid, mind, character));
        }

        var warps = new List<EntityUid>();
        var warpQuery = EntityManager.EntityQueryEnumerator<ESStagehandAwareComponent, WarpPointComponent>();
        while (warpQuery.MoveNext(out var uid, out _, out _))
        {
            warps.Add(uid);
        }

        var orderedMinds = minds
            .OrderBy(m => _mask?.GetTroupeOrNull((m, m.Comp1)))
            .ThenBy(m => m.Comp2.Name);

        var grp = new ButtonGroup();
        foreach (var mind in orderedMinds)
        {
            var button = new ESObservablePlayerButton
            {
                Group = grp,
            };
            button.SetPlayer(mind);
            button.OnPressed += _ =>
            {
                SetInfoPanel(observe, mind);
            };
            observe.MindsContainer.AddChild(button);
        }

        var orderedWarps = warps
            .OrderBy(e => EntityManager.GetComponent<MetaDataComponent>(e).EntityName);

        foreach (var warp in orderedWarps)
        {
            var button = new ESObservablePlayerButton();
            button.ToggleMode = false;
            button.SetEntity(warp);
            button.OnPressed += _ =>
            {
                observe.InvokeWarpForEntity(warp);
            };
            observe.MindsContainer.AddChild(button);
        }
    }

    public void UpdateInfoPanel(ESStagehandObserveControl observe)
    {
        if (!EntityManager.TryGetComponent<MindComponent>(observe.CurrentEntity, out var mind) ||
            !EntityManager.TryGetComponent<ESCharacterComponent>(observe.CurrentEntity, out var character))
            return;

        SetInfoPanel(observe, (observe.CurrentEntity.Value, mind, character));
    }

    public void SetInfoPanel(ESStagehandObserveControl observe, Entity<MindComponent, ESCharacterComponent> ent)
    {
        if (EntityManager.Deleted(ent))
            return;

        observe.PlayerInfoContainer.Visible = true;

        var (uid, mind, character) = ent;
        observe.CurrentEntity = uid;
        observe.WarpButton.Disabled = mind.CurrentEntity == null; // See ESStagehandSystem.cs

        var mask = _mask?.GetMaskOrNull((uid, mind));
        var troupe = _mask?.GetTroupeOrNull((uid, mind));

        observe.NameLabel.UnsafeSetMarkup(Loc.GetString("es-observe-menu-label-name-big", ("text", character.Name)));

        if (_job.MindTryGetJob(uid, out var job))
            observe.JobLabel.UnsafeSetMarkup(Loc.GetString("es-observe-menu-label-job", ("text", job.LocalizedName)));

        if (_prototype.TryIndex(mask, out var maskPrototype))
        {
            observe.MaskLabel.UnsafeSetMarkup(
                Loc.GetString("es-observe-menu-label-name-big", ("text", Loc.GetString(maskPrototype.Name))),
                maskPrototype.Color);
        }

        if (_prototype.TryIndex(troupe, out var troupePrototype))
        {
            observe.TroupeLabel.UnsafeSetMarkup(
                Loc.GetString("es-observe-menu-label-fmt", ("text", Loc.GetString(troupePrototype.Name))),
                troupePrototype.Color);
        }

        if (_objective == null)
            return;

        observe.ObjectiveContainer.Children.Clear();
        foreach (var objective in _objective.GetObjectives(uid))
        {
            var ctrl = new ESObjectiveControl();
            ctrl.SetObjective(objective);
            observe.ObjectiveContainer.AddChild(ctrl);
        }
    }
}
