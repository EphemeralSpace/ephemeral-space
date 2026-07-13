using System.Diagnostics.CodeAnalysis;
using Content.Shared._ES.Breakable.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Repairable;
using Content.Shared.UserInterface;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._ES.Breakable;

public sealed partial class ESBreakableSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private NameModifierSystem _nameModifier = default!;

    [Dependency] private EntityQuery<ESBreakableComponent> _breakableQuery = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESBreakableComponent, RefreshNameModifiersEvent>(OnRefreshNameModifiers);
        SubscribeLocalEvent<ESBreakableComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<ESBreakableComponent, DamageChangedEvent>(OnDamageChanged);

        SubscribeLocalEvent<ESBreakableActivatableUiComponent, ActivatableUIOpenAttemptEvent>(OnOpenAttempt);
        SubscribeLocalEvent<ESBreakableDeviceNetworkComponent, BeforePacketSentEvent>(OnBeforePacketSent);
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
        // Necessary guard since this event gets raised in weird contexts.
        if (_timing.ApplyingState)
            return;

        SetBroken(ent.AsNullable(), _damageable.GetDamage((ent, args.Damageable)).GetTotal() >= ent.Comp.Threshold);
    }

    private void OnOpenAttempt(Entity<ESBreakableActivatableUiComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (IsBroken(ent.Owner))
            args.Cancel();
    }

    private void OnBeforePacketSent(Entity<ESBreakableDeviceNetworkComponent> ent, ref BeforePacketSentEvent args)
    {
        if (IsBroken(ent.Owner))
            args.Cancel();
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

        // TODO: Because audio prediction is hacky garbage i'm going to do this.
        // Otherwise, every single unpredicted damage source is going to not play audio properly.
        if (broken && _net.IsServer)
            _audio.PlayPvs(ent.Comp.Sound, Transform(ent).Coordinates);

        _nameModifier.RefreshNameModifiers(ent.Owner);
        _appearance.SetData(ent, ESBreakableVisuals.Broken, broken);

        var ev = new ESBrokenStateChanged(broken);
        RaiseLocalEvent(ent, ref ev);

        return true;
    }

    public bool TryGetBrokenThreshold(Entity<ESBreakableComponent?> ent, [NotNullWhen(true)] out FixedPoint2? threshold)
    {
        threshold = null;
        if (!_breakableQuery.Resolve(ent, ref ent.Comp, false))
            return false;

        threshold = ent.Comp.Threshold;
        return true;
    }
}

/// <summary>
/// Event raised on an entity with <see cref="ESBreakableComponent"/> when it either breaks or is repaired.
/// </summary>
[ByRefEvent]
public readonly record struct ESBrokenStateChanged(bool Broken);
