using Robust.Shared.GameStates;

namespace Content.Shared._ES.Stagehand.Components;

/// <summary>
/// Marker component used to indicate things that should always be sent to stagehands
/// Minds, objectives, etc.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESStagehandAwareSystem))]
public sealed partial class ESStagehandAwareComponent : Component;
