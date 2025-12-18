using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;

namespace Content.Shared._ES.TileFires;

/// <summary>
///     Shared API for spawning tile fires.
///     See serverside system for actual growth logic.
/// </summary>
public sealed class ESTileFireSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        // appearance on startup
        SubscribeLocalEvent<FlammableComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<FlammableComponent> ent, ref ComponentStartup args)
    {
        var flammable = ent.Comp;
        // not done in flammablesys because no shared and i want this in entity spawn menu man idk
        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        _appearance.SetData(ent, FireVisuals.OnFire, flammable.OnFire, appearance);
        _appearance.SetData(ent, FireVisuals.FireStacks, (int) MathF.Floor(flammable.FireStacks / flammable.FirestackVisualDivisor), appearance);
    }
}
