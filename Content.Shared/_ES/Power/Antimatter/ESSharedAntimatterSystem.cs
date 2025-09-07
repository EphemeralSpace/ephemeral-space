using Content.Shared._ES.Power.Antimatter.Components;
using Content.Shared.Destructible;
using Content.Shared.Examine;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Shared._ES.Power.Antimatter;

public abstract class ESSharedAntimatterSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly SharedPointLightSystem PointLight = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESAntimatterConverterComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ESAntimatterConverterComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
        SubscribeLocalEvent<ESAntimatterConverterComponent, BreakageEventArgs>(OnBreakage);
        SubscribeLocalEvent<ESAntimatterConverterComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<ESAntimatterConverterComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.Broken)
            args.PushMarkup(Loc.GetString("es-antimatter-converter-examine-broken"));
    }

    private void OnMapInit(Entity<ESAntimatterConverterComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdateTime = Timing.CurTime + TimeSpan.FromSeconds(1);
    }

    private void OnAnchorStateChanged(Entity<ESAntimatterConverterComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            return;
        Appearance.SetData(ent, ESAntimatterConverterVisuals.Draining, false);
        PointLight.SetEnabled(ent, false);
    }

    private void OnBreakage(Entity<ESAntimatterConverterComponent> ent, ref BreakageEventArgs args)
    {
        ent.Comp.Broken = true;
        Dirty(ent);

        Appearance.SetData(ent, ESAntimatterConverterVisuals.Broken, true);
        Appearance.SetData(ent, ESAntimatterConverterVisuals.Draining, false);
        PointLight.SetEnabled(ent, false);
    }

    [PublicAPI]
    public virtual void SetMass(Entity<ESAntimatterComponent> ent, float mass)
    {
        ent.Comp.Mass = mass;

        if (mass <= 0)
        {
            PredictedQueueDel(ent.Owner);
        }
    }
}
