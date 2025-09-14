using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Client._ES.Lighting;

/// <summary>
///     Handles enabling and disabling mob inherent pointlights when locally attaching to a new mob.
/// </summary>
public sealed class ESInherentLightSystem : EntitySystem
{
    [Dependency] private readonly PointLightSystem _light = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttach);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetach);
    }

    private void OnPlayerAttach(LocalPlayerAttachedEvent ev)
    {
        if (!TryComp<ESInherentLightComponent>(ev.Entity, out var light)
            || light.LightEntity != null)
            return;

        // Don't enable inherent light if the mob already has a pointlight on itself
        if (TryComp<PointLightComponent>(ev.Entity, out var selfPointLight) && selfPointLight.Enabled)
            return;

        light.LightEntity = SpawnAttachedTo(light.LightPrototype, new EntityCoordinates(ev.Entity, Vector2.Zero));
        _light.SetEnabled(light.LightEntity.Value, true);
    }

    private void OnPlayerDetach(LocalPlayerDetachedEvent ev)
    {
        if (!TryComp<ESInherentLightComponent>(ev.Entity, out var light)
            || light.LightEntity == null
            || !TryComp<PointLightComponent>(light.LightEntity, out var inherentPointLight))
            return;

        QueueDel(light.LightEntity);
        light.LightEntity = null;
    }
}
