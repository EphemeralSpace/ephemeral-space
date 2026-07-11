using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Barricade.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESBarricadeKitSystem))]
public sealed partial class ESBarricadeKitComponent : Component
{
    /// <summary>
    ///     Full-tile barricade created when used in hand.
    /// </summary>
    [DataField]
    public EntProtoId TileBarricade = "Barricade";

    /// <summary>
    ///     Barricade created when used on an airlock.
    /// </summary>
    [DataField]
    public EntProtoId AirlockBarricade = "BarricadeBlock";

    [DataField]
    public TimeSpan SetupDelay = TimeSpan.FromSeconds(3f);
}

public enum ESBarricadeKitBarricadeType
{
    Tile,
    Airlock
}

[Serializable, NetSerializable]
public sealed partial class ESBarricadeDoAfterEvent : SimpleDoAfterEvent
{
    public ESBarricadeKitBarricadeType Type;
}
