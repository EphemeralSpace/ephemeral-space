using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Cargo.Storeroom.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSharedStoreroomSystem))]
public sealed partial class ESStoreroomPalletComponent : Component
{
    [DataField]
    public EntityWhitelist GoodsWhitelist;
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class ESStoreroomContainerEntry : IEquatable<ESStoreroomContainerEntry>
{
    [DataField] public EntProtoId? IconEntity;
    [DataField] public string Name = string.Empty;

    [DataField] public List<ESStoreroomEntry> Contents = new();

    public ESStoreroomContainerEntry(EntProtoId? iconEntity, string name)
    {
        IconEntity = iconEntity;
        Name = name;
    }

    public bool Equals(ESStoreroomContainerEntry? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return Name == other.Name && Contents.Count == 0 && other.Contents.Count == 0;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is ESStoreroomContainerEntry other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name);
    }
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class ESStoreroomEntry : IEquatable<ESStoreroomEntry>
{
    [DataField] public EntProtoId? IconEntity;
    [DataField] public string Name = string.Empty;

    [DataField] public int Count = 1;

    public ESStoreroomEntry(EntProtoId? iconEntity, string name)
    {
        IconEntity = iconEntity;
        Name = name;
    }

    public bool Equals(ESStoreroomEntry? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Nullable.Equals(IconEntity, other.IconEntity) && Name == other.Name && Count == other.Count;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is ESStoreroomEntry other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(IconEntity, Name);
    }
}
