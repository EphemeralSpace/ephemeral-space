using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Masks.Phantom.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESPhantomSystem))]
public sealed partial class ESPhantomComponent : Component
{
    /// <summary>
    /// Prototype for <see cref="ActionEntity"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId<ActionComponent> MaterializeAction = "ESActionPhantomMaterialize";

    /// <summary>
    /// Entity for <see cref="MaterializeAction"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// Cooldown applied to <see cref="ActionEntity"/> after exiting materialized form
    /// </summary>
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromMinutes(5);
}

[Serializable, NetSerializable]
public enum ESPhantomVisuals : byte
{
    Materialized,
}

public sealed partial class ESPhantomMaterializeActionEvent : InstantActionEvent;
