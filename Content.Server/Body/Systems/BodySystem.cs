using System.Numerics;
using Content.Server.Ghost;
using Content.Server.Humanoid;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Timing;

namespace Content.Server.Body.Systems;

public sealed partial class BodySystem : SharedBodySystem
{
    [Dependency] private GhostSystem _ghostSystem = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private HumanoidAppearanceSystem _humanoidSystem = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedMindSystem _mindSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
    }

    private void OnApplyMetabolicMultiplier(
        Entity<BodyComponent> ent,
        ref ApplyMetabolicMultiplierEvent args)
    {
        foreach (var organ in GetBodyOrgans(ent, ent))
        {
            RaiseLocalEvent(organ.Id, ref args);
        }
    }

    protected override void AddPart(
        Entity<BodyComponent?> bodyEnt,
        Entity<BodyPartComponent> partEnt,
        string slotId)
    {
        // TODO: Predict this probably.
        base.AddPart(bodyEnt, partEnt, slotId);

        var layer = partEnt.Comp.ToHumanoidLayers();
        if (layer != null)
        {
            var layers = HumanoidVisualLayersExtension.Sublayers(layer.Value);
            _humanoidSystem.SetLayersVisibility(bodyEnt.Owner, layers, visible: true);
        }
    }

    protected override void RemovePart(
        Entity<BodyComponent?> bodyEnt,
        Entity<BodyPartComponent> partEnt,
        string slotId)
    {
        base.RemovePart(bodyEnt, partEnt, slotId);

        if (!TryComp<HumanoidAppearanceComponent>(bodyEnt, out var humanoid))
            return;

        var layer = partEnt.Comp.ToHumanoidLayers();

        if (layer is null)
            return;

        var layers = HumanoidVisualLayersExtension.Sublayers(layer.Value);
        _humanoidSystem.SetLayersVisibility((bodyEnt, humanoid), layers, visible: false);
    }
}
