using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Mousetrap;

/// <summary>
/// Component inteded to be used for mouse traps.
/// Will stop step triggers from happening unless armed via <see cref="Item.ItemToggle.Components.ItemToggleComponent"/>
/// and will scale damage taken from <see cref="Trigger.Components.Effects.DamageOnTriggerComponent"/>
/// depending on mass.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MousetrapComponent : Component
{
    /// <summary>
    /// Additional damage applied to entities with <see cref="ESMousetrapPestComponent"/>
    /// </summary>
    [DataField]
    public DamageSpecifier MouseDamage = new();
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ESMousetrapPestComponent : Component;
