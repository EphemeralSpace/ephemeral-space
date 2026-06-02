using Content.Shared._ES.Food;
using Content.Shared.Alert;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Rejuvenate;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Nutrition.EntitySystems;

public sealed partial class HungerSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private MovementSpeedModifierSystem _movementSpeedModifier = default!;

    private static readonly ProtoId<AlertCategoryPrototype> HungerAlertCategory = "Hunger";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HungerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<HungerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<HungerComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMoveSpeed);
        SubscribeLocalEvent<HungerComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<HungerComponent, IngestionAttemptEvent>(OnIngesting);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HungerComponent>();
        while (query.MoveNext(out var uid, out var hunger))
        {
            if (hunger.NextDecayTime is null || _timing.CurTime < hunger.NextDecayTime)
                continue;

            // decay hunger by 1 and reset time
            // it clamps but check anyway to reduce work
            if (hunger.CurrentHunger != HungerThreshold.Starving)
                ModifySatiety((uid, hunger), -1);

            hunger.NextDecayTime = _timing.CurTime + hunger.HungerDecayTime;
        }
    }

    private void OnMapInit(EntityUid uid, HungerComponent component, MapInitEvent args)
    {
        UpdateAlerts((uid, component));
        component.NextDecayTime = _timing.CurTime + component.HungerDecayTime;
    }

    private void OnShutdown(EntityUid uid, HungerComponent component, ComponentShutdown args)
    {
        _alerts.ClearAlertCategory(uid, HungerAlertCategory);
    }

    private void OnRefreshMoveSpeed(EntityUid uid, HungerComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (!component.HungerThresholdSlowdown.TryGetValue(component.CurrentHunger, out var modifier))
            return;

        args.ModifySpeed(modifier, modifier);
    }

    private void OnRejuvenate(EntityUid uid, HungerComponent component, RejuvenateEvent args)
    {
        SetHunger((uid, component), HungerThreshold.Okay);
    }

    private void OnIngesting(Entity<HungerComponent> ent, ref IngestionAttemptEvent args)
    {
        if (!TryComp<ESFoodComponent>(args.Ingested, out var food))
            return;

        if (food.SatietyMultiplier <= 0 || ent.Comp.CurrentHunger != HungerThreshold.Okay)
            return;

        args.Cancelled = true;
        args.Blocker = ent.Owner;
        args.Popup = "ingestion-already-full";
    }

    /// <summary>
    ///     Gets the current hunger value of the given entity, returning null if they do not have hunger
    /// </summary>
    public HungerThreshold? GetHunger(Entity<HungerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return null;

        return ent.Comp.CurrentHunger;
    }

    /// <summary>
    ///     Adds to the current hunger of an entity by the specified value
    ///     Modifying satiety because +1 is less hunger, not more
    /// </summary>
    public void ModifySatiety(Entity<HungerComponent?> ent, int amount)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        SetHunger(ent, (int) ent.Comp.CurrentHunger + amount);
    }

    // no reason to call this externally
    private void SetHunger(Entity<HungerComponent?> ent, int amount)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        // clamp between enum values properly
        amount = Math.Clamp(amount, 0, (int)HungerThreshold.Okay);
        SetHunger(ent, (HungerThreshold) amount);
    }

    /// <summary>
    ///     Sets the current hunger of an entity to the specified threshold
    /// </summary>
    public void SetHunger(Entity<HungerComponent?> ent, HungerThreshold threshold)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.CurrentHunger = threshold;

        UpdateAlerts(ent);
        ent.Comp.NextDecayTime = _timing.CurTime + ent.Comp.HungerDecayTime;
        _movementSpeedModifier.RefreshMovementSpeedModifiers(ent);

        Dirty(ent);
    }

    /// <summary>
    ///     Returns true if the entity is below a hunger threshold.
    /// </summary>
    public bool IsHungerBelowState(Entity<HungerComponent?> ent, HungerThreshold threshold)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false; // It's never going to go hungry, so it's probably fine to assume that it's not... you know, hungry.

        return ent.Comp.CurrentHunger < threshold;
    }

    private void UpdateAlerts(Entity<HungerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var uid = ent.Owner;
        var component = ent.Comp;
        if (component.HungerThresholdAlerts.TryGetValue(component.CurrentHunger, out var alertId))
        {
            _alerts.ShowAlert(uid, alertId);
        }
        else
        {
            _alerts.ClearAlertCategory(uid, HungerAlertCategory);
        }
    }
}
