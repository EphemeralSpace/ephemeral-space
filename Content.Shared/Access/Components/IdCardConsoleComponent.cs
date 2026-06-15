using Content.Shared.Access.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Access.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedIdCardConsoleSystem))]
public sealed partial class IdCardConsoleComponent : Component
{
    public static string TargetIdCardSlotId = "IdCardConsole-targetId";

    [DataField]
    public ItemSlot TargetIdSlot = new();

    [Serializable, NetSerializable]
    public sealed class WriteToTargetIdMessage : BoundUserInterfaceMessage
    {
        public readonly string FullName;
        public readonly string JobTitle;
        public readonly ProtoId<JobPrototype> JobPrototype;

        public WriteToTargetIdMessage(string fullName, string jobTitle, ProtoId<JobPrototype> jobPrototype)
        {
            FullName = fullName;
            JobTitle = jobTitle;
            JobPrototype = jobPrototype;
        }
    }

    [Serializable, NetSerializable]
    public sealed class IdCardConsoleBoundUserInterfaceState : BoundUserInterfaceState
    {
        public bool IsTargetIdPresent => Card.HasValue;
        public readonly string? TargetIdFullName;
        public readonly string? TargetIdJobTitle;
        public readonly ProtoId<JobPrototype> TargetIdJobPrototype;
        public readonly NetEntity? Card;

        public IdCardConsoleBoundUserInterfaceState(
            string? targetIdFullName,
            string? targetIdJobTitle,
            ProtoId<JobPrototype> targetIdJobPrototype,
            NetEntity? card)
        {
            TargetIdFullName = targetIdFullName;
            TargetIdJobTitle = targetIdJobTitle;
            TargetIdJobPrototype = targetIdJobPrototype;
            Card = card;
        }
    }

    [Serializable, NetSerializable]
    public enum IdCardConsoleUiKey : byte
    {
        Key,
    }
}
