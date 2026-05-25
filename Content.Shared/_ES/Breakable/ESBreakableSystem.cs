using Content.Shared._ES.Breakable.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.Examine;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Repairable;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Breakable;

public sealed partial class ESBreakableSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private NameModifierSystem _nameModififer = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESBreakableComponent, RefreshNameModifiersEvent>(OnRefreshNameModifiers);
        SubscribeLocalEvent<ESBreakableComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<ESBreakableComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<ESBreakableComponent, BreakageEventArgs>(OnBreakage);
        SubscribeLocalEvent<ESBreakableComponent, RepairedEvent>(OnRepaired);
    }

    private void OnRefreshNameModifiers(Entity<ESBreakableComponent> ent, ref RefreshNameModifiersEvent args)
    {
        if (ent.Comp.Broken)
            args.AddModifier("es-broken-name-prefix");
    }

    private void OnExamined(Entity<ESBreakableComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.Broken)
            return;

        if (!TryComp<RepairableComponent>(ent, out var repairable))
            return;

        var repairQuality = _prototype.Index(repairable.QualityNeeded);
        args.PushMarkup(Loc.GetString("es-breakable-broken-examine", ("tool", Loc.GetString(repairQuality.ToolName))));
    }

    private void OnDamageChanged(Entity<ESBreakableComponent> ent, ref DamageChangedEvent args)
    {
        if (ent.Comp.Threshold is null)
            return;

        SetBroken(ent.AsNullable(), _damageable.GetDamage((ent, args.Damageable)).GetTotal() >= ent.Comp.Threshold);
    }

    private void OnBreakage(Entity<ESBreakableComponent> ent, ref BreakageEventArgs args)
    {
        SetBroken(ent.AsNullable(), true);
    }

    private void OnRepaired(Entity<ESBreakableComponent> ent, ref RepairedEvent args)
    {
        SetBroken(ent.AsNullable(), false);
    }

    /// <summary>
    /// Checks if a given entity is broken according to <see cref="ESBreakableComponent"/>
    /// </summary>
    public bool IsBroken(Entity<ESBreakableComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        return ent.Comp.Broken;
    }

    /// <summary>
    /// Sets the broken state of an entity with <see cref="ESBreakableComponent"/>, raising <see cref="ESBrokenStateChanged"/> if it changed.
    /// </summary>
    public bool SetBroken(Entity<ESBreakableComponent?> ent, bool broken)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (ent.Comp.Broken == broken)
            return false;

        ent.Comp.Broken = broken;
        Dirty(ent);

        _nameModififer.RefreshNameModifiers(ent.Owner);
        _appearance.SetData(ent, ESBreakableVisuals.Broken, broken);

        var ev = new ESBrokenStateChanged(broken);
        RaiseLocalEvent(ent, ref ev);

        return true;
    }
}

/// <summary>
/// Event raised on an entity with <see cref="ESBreakableComponent"/> when it either breaks or is repaired.
/// </summary>
[ByRefEvent]
public readonly record struct ESBrokenStateChanged(bool Broken);
