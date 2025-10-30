using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Spawner.Components;

/// <summary>
/// Marker used to define locations for <see cref="ESDistributedSpawnerComponent"/>
/// </summary>
[RegisterComponent, EntityCategory("Spawner")]
[Access(typeof(ESSpawnerSystem))]
public sealed partial class ESDistributedSpawnerMarkerComponent : Component
{
    /// <summary>
    /// ID matched with <see cref="ESDistributedSpawnerComponent.Id"/> for associating it with a spawner
    /// </summary>
    [DataField(required: true)]
    public ProtoId<TagPrototype> Id;
}
