namespace Content.Shared._ES.TileFires;

[RegisterComponent]
public sealed partial class ESTileFireOriginComponent : Component
{
    [DataField]
    public List<EntityUid> Fires = new();
}
