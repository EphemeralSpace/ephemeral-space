using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Overlays;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

public sealed partial class ShowHungerIconsSystem : EquipmentHudSystem<ShowHungerIconsComponent>
{
    [Dependency] private IPrototypeManager _proto = default!;

    private static readonly Dictionary<HungerThreshold, ProtoId<SatiationIconPrototype>?> StatusIcons = new()
    {
        { HungerThreshold.Starving, "HungerIconStarving" },
        { HungerThreshold.Hungry, "HungerIconPeckish" },
        { HungerThreshold.Peckish, "HungerIconPeckish" },
        { HungerThreshold.Okay, null },
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HungerComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    private void OnGetStatusIconsEvent(EntityUid uid, HungerComponent component, ref GetStatusIconsEvent ev)
    {
        if (!IsActive)
            return;

        if (StatusIcons.TryGetValue(component.CurrentHunger, out var icon)
            && icon != null && _proto.TryIndex(icon, out var proto))
            ev.StatusIcons.Add(proto);
    }
}
