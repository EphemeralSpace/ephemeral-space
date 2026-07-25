using Content.Shared._ES.Objectives.Components;

namespace Content.Shared._ES.Objectives;

public abstract partial class ESSharedObjectiveSystem
{
    private void InitializeCounter()
    {
        SubscribeLocalEvent<ESCounterObjectiveComponent, ESInitializeObjectiveEvent>(OnInitializeObjective);
        SubscribeLocalEvent<ESCounterObjectiveComponent, ESGetObjectiveProgressEvent>(OnCounterGetProgress);

        SubscribeLocalEvent<ESPopulationProportionCounterObjectiveComponent, ESGetCounterObjectiveTargetEvent>(OnGetPopulationProportionCounterObjectiveTarget);
    }

    private void OnInitializeObjective(Entity<ESCounterObjectiveComponent> ent, ref ESInitializeObjectiveEvent args)
    {
        var ev = new ESGetCounterObjectiveTargetEvent();
        RaiseLocalEvent(ent, ref ev);
        if (ev.Handled)
        {
            SetObjectiveCounterTarget(ent.AsNullable(), ev.Target);
        }
        else if (ent.Comp.MaxTarget is { } maxTarget)
        {
            // Generate a random value on [target, maxTarget] in chunks of targetIncrement
            var range = maxTarget - ent.Comp.Target;
            var incrementCount = (int) Math.Ceiling(range / ent.Comp.TargetIncrement);
            var blend = _random.Next(0, incrementCount + 1); // non-inclusive right bound adjustment
            var target = Math.Clamp(ent.Comp.Target + blend * ent.Comp.TargetIncrement, ent.Comp.Target, maxTarget);
            SetObjectiveCounterTarget(ent.AsNullable(), target);
        }
        else
        {
            SetObjectiveCounterTarget(ent.AsType(), ent.Comp.Target);
        }
    }

    private void OnCounterGetProgress(Entity<ESCounterObjectiveComponent> ent, ref ESGetObjectiveProgressEvent args)
    {
        args.Progress = ent.Comp.Counter / ent.Comp.Target;
    }

    private void OnGetPopulationProportionCounterObjectiveTarget(Entity<ESPopulationProportionCounterObjectiveComponent> ent, ref ESGetCounterObjectiveTargetEvent args)
    {
        args.Target = Math.Clamp(MathF.Round(_player.PlayerCount * ent.Comp.Proportion), ent.Comp.Minimum, ent.Comp.Maximum);
        args.Handled = true;
    }

    /// <summary>
    /// Returns the counter's target value.
    /// If there's no target, returns -1
    /// </summary>
    public float GetObjectiveCounterTarget(Entity<ESCounterObjectiveComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return -1;

        return ent.Comp.Target;
    }

    /// <summary>
    /// Sets the counter's target value, updating entity name and description.
    /// </summary>
    public void SetObjectiveCounterTarget(Entity<ESCounterObjectiveComponent?> ent, float value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Target = value;

        // Initialize name and description
        if (ent.Comp.Title != null)
            _metaData.SetEntityName(ent, Loc.GetString(ent.Comp.Title, ("count", ent.Comp.Target)));
        if (ent.Comp.Description != null)
            _metaData.SetEntityDescription(ent, Loc.GetString(ent.Comp.Description, ("count", ent.Comp.Target)));
        Dirty(ent);
    }

    /// <summary>
    /// Adjusts the counter for the objective by <see cref="val"/>
    /// </summary>
    /// <param name="ent">Objective entity</param>
    /// <param name="val">How much to add or remove from the counter</param>
    public void AdjustObjectiveCounter(Entity<ESObjectiveComponent?, ESCounterObjectiveComponent?> ent, float val = 1)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2))
            return;

        SetObjectiveCounter(ent, ent.Comp2.Counter + val);
    }

    /// <summary>
    /// Adjusts the counter for all objectives with component <see cref="T"/>
    /// </summary>
    /// <param name="val">How much to add or remove from the counter</param>
    public void AdjustObjectiveCounter<T>(float val = 1) where T : Component
    {
        foreach (var objective in GetObjectives<T>())
        {
            AdjustObjectiveCounter((objective.Owner, objective.Comp2), val);
        }
    }

    /// <summary>
    /// Sets the counter for the objective to <see cref="val"/>
    /// </summary>
    /// <param name="ent">Objective entity</param>
    /// <param name="val">Value to set the counter to</param>
    public void SetObjectiveCounter(Entity<ESObjectiveComponent?, ESCounterObjectiveComponent?> ent, float val)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2))
            return;

        var clampedVal = Math.Max(val, 0f);
        if (MathHelper.CloseTo(clampedVal, ent.Comp2.Counter)) // Same value
            return;

        // Don't allow counters to go into the negatives.
        ent.Comp2.Counter = clampedVal;
        Dirty(ent, ent.Comp2);

        RefreshObjectiveProgress((ent, ent));
    }
}
