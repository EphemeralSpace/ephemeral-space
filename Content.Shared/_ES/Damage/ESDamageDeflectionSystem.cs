using Content.Shared._ES.Damage.Components;
using Content.Shared.Damage.Systems;

namespace Content.Shared._ES.Damage;

/// <summary>
/// This handles <see cref="ESDamageDeflectionComponent"/>
/// </summary>
public sealed class ESDamageDeflectionSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESDamageDeflectionComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
    }

    private void OnBeforeDamageChanged(Entity<ESDamageDeflectionComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (args.Damage.GetTotal() < ent.Comp.Threshold)
            args.Cancelled = true;
    }
}
