using Content.Shared.Inventory;

namespace Content.Client._ES.Viewcone;


public sealed class GetViewconeEvent : EntityEventArgs, IInventoryRelayEvent
{
    public float ConeAngle;
    public float ConeFeather;
    public float ConeIgnoreRadius;
    public float ConeIgnoreFeather;

    public GetViewconeEvent(float coneAngle, float coneFeather, float coneIgnoreRadius, float coneIgnoreFeather)
    {
        ConeAngle = coneAngle;
        ConeFeather = coneFeather;
        ConeIgnoreRadius = coneIgnoreRadius;
        ConeIgnoreFeather = coneIgnoreFeather;
    }

    public SlotFlags TargetSlots => SlotFlags.HEAD | SlotFlags.MASK | SlotFlags.EYES;
}
