using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.DoAfter;
using Content.Shared.Inventory;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._DV.NodeCrawl;

/// <summary>
/// Handles entities that can enter and exit node-constrained movement.
/// </summary>
// todo this should probably support scoping based on nodegroup id once we get pipecrawlers.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedNodeCrawlSystem))]
public sealed partial class NodeCrawlerComponent : Component
{
    /// <summary>
    /// The mover this crawler is currently being carried by, if any
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Mover;

    /// <summary>
    /// The crawl action to add to this entity on startup.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<ActionComponent> Action;

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// Components of entities to reveal while inside a mover
    /// </summary>
    [DataField(readOnly: true)]
    public Type[] RevealedComponents;

    /// <summary>
    /// Whitelist for entities that will be considered as exit nodes.
    /// </summary>
    [DataField]
    public EntityWhitelist? ExitNodes;

    /// <summary>
    /// The entity to spawn and use as a node mover.
    /// </summary>
    [DataField]
    public EntProtoId MoverEntity = "NodeCrawlMoverEntity";

    /// <summary>
    /// How long it takes to enter a node.
    /// </summary>
    [DataField]
    public TimeSpan EnterDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// If an entity has an inventory, these slots must be empty in order to start crawling.
    /// </summary>
    [DataField]
    public SlotFlags RequiredEmptySlots = SlotFlags.BACK;

    /// <summary>
    /// If an entity has hands, do these hands need to be empty
    /// </summary>
    [DataField]
    public bool RequireEmptyHands = true;

    /// <summary>
    /// The popup message to show to an entity with <see cref="RequireEmptyHands"/> when they are holding something
    /// </summary>
    [DataField]
    public LocId RequireEmptyHandsPopupMessage = "es-nodecrawl-popup-no-held";

    /// <summary>
    /// The popup message to show to an entity that's wearing something in <see cref="RequiredEmptySlots"/>.
    /// </summary>
    [DataField]
    public LocId EmptySlotsPopupMessage = "es-nodecrawl-popup-no-jumpsuit-backpack";

    /// <summary>
    /// Sound played when starting a nodecrawl.
    /// </summary>
    [DataField]
    public SoundSpecifier? StartSound = new SoundCollectionSpecifier("trashBagRustle");

    /// <summary>
    /// Played when this crawler is forcibly pulled out of their crawl for whatever reason
    /// e.g. being hit by an object in transit while disposals crawling.
    /// </summary>
    [DataField]
    public SoundSpecifier? ForceStopSound;
}

public sealed partial class StartNodeCrawlActionEvent : EntityTargetActionEvent;

[Serializable, NetSerializable]
public sealed partial class NodeCrawlEnterDoAfterEvent : SimpleDoAfterEvent;

[ByRefEvent]
public readonly record struct NodeCrawlerStartedCrawlingEvent(Entity<NodeCrawlerMovementComponent> Mover);

