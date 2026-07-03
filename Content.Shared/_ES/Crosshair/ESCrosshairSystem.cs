using Content.Shared.CombatMode;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using JetBrains.Annotations;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Crosshair;

public sealed partial class ESCrosshairSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedCombatModeSystem _combat = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedTransformSystem _xform = default!;

    private static readonly EntProtoId CrosshairEffect = "ESCrosshairEffect";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESCrosshairProviderComponent, HandDeselectedEvent>(OnHandDeselected);
        SubscribeLocalEvent<ESCrosshairProviderComponent, HandSelectedEvent>(OnHandSelected);
        SubscribeLocalEvent<ESCrosshairAimerComponent, CombatModeToggledEvent>(OnCombatModeToggled);
        SubscribeLocalEvent<ESCrosshairAimerComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<ESCrosshairAimerComponent, EntityTerminatingEvent>(OnAimerTerminating);

        SubscribeAllEvent<ESCrosshairNetworkEvent>(OnCrosshair);
    }

    #region Events / API

    private void OnHandDeselected(Entity<ESCrosshairProviderComponent> ent, ref HandDeselectedEvent args)
    {
        if (!TryComp<ESCrosshairAimerComponent>(args.User, out var provider))
            return;

        SetCrosshair((args.User, provider), false);
    }

    private void OnHandSelected(Entity<ESCrosshairProviderComponent> ent, ref HandSelectedEvent args)
    {
        if (!TryComp<ESCrosshairAimerComponent>(args.User, out var provider))
            return;

        if (!ent.Comp.RequiresCombatMode || _combat.IsInCombatMode(args.User))
        {
            SetCrosshair((args.User, provider), true);
        }
    }

    private void OnCombatModeToggled(Entity<ESCrosshairAimerComponent> ent, ref CombatModeToggledEvent args)
    {
        if (!_hands.TryGetActiveItem(ent.Owner, out var item)
            || !TryComp<ESCrosshairProviderComponent>(item, out var provider))
        {
            SetCrosshair(ent.AsNullable(), false);
            return;
        }

        var valid = args.Enabled || !provider.RequiresCombatMode;
        SetCrosshair(ent.AsNullable(), valid);
    }

    private void OnMobStateChanged(Entity<ESCrosshairAimerComponent> ent, ref MobStateChangedEvent args)
    {
        // we dont worry about the inverse (going from crit to alive or whatever)
        // cuz in that case they arent gonna be holding a gun or shit anyway
        // so i dont think its realistically possible for them to be in an invalid state.
        if (args.NewMobState is not (MobState.Critical or MobState.Dead))
            return;

        SetCrosshair(ent.AsNullable(), false);
    }

    private void OnAimerTerminating(Entity<ESCrosshairAimerComponent> ent, ref EntityTerminatingEvent args)
    {
        var crosshair = ent.Comp.CrosshairEntity;
        if (crosshair is not null && !TerminatingOrDeleted(crosshair))
        {
            QueueDel(crosshair);
        }
    }

    [PublicAPI]
    public void SetCrosshair(Entity<ESCrosshairAimerComponent?> entity, bool enabled)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        if (entity.Comp.CrosshairEntity is not null && enabled
            || entity.Comp.CrosshairEntity is null && !enabled)
            return;

        if (enabled)
        {
            // todo this could probably get reused for npcs to instead point at whatever their target is somehow
            // but for now no
            if (!HasComp<ActorComponent>(entity))
                return;

            entity.Comp.CrosshairEntity = PredictedSpawnAtPosition(CrosshairEffect, Transform(entity).Coordinates);
            var comp = new ESCrosshairEntityComponent() { User = entity.Owner };
            AddComp(entity.Comp.CrosshairEntity.Value, comp);
            _appearance.SetData(entity.Comp.CrosshairEntity.Value, ESCrosshairVisuals.Name, Identity.Name(entity.Owner, EntityManager));
        }
        else
        {
            PredictedQueueDel(entity.Comp.CrosshairEntity);
            entity.Comp.CrosshairEntity = null;
        }

        Dirty(entity);
    }

    #endregion

    private void OnCrosshair(ESCrosshairNetworkEvent msg, EntitySessionEventArgs args)
    {
        if (!msg.Coordinates.Position.IsValid())
            return;

        if (args.SenderSession.AttachedEntity is not { } ent || !TryComp<ESCrosshairAimerComponent>(ent, out var aimer))
            return;

        var userXform = Transform(ent);
        if (aimer.CrosshairEntity is not { } crosshairEntity || userXform.MapUid is not { } map)
            return;

        var crosshairXform = Transform(crosshairEntity);

        _xform.SetParent(crosshairEntity, crosshairXform, map);
        _xform.SetLocalPosition(crosshairEntity, msg.Coordinates.Position, crosshairXform);
    }
}
