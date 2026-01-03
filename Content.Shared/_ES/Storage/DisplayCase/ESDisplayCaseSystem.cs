using Content.Shared._ES.Storage.DisplayCase.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.Examine;

namespace Content.Shared._ES.Storage.DisplayCase;

public sealed class ESDisplayCaseSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESDisplayCaseComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ESDisplayCaseComponent, BreakageEventArgs>(OnBreakage);
        SubscribeLocalEvent<ESDisplayCaseComponent, ExaminedEvent>(OnExamined);
    }

    private void OnStartup(Entity<ESDisplayCaseComponent> ent, ref ComponentStartup args)
    {
        UpdateSlotLock(ent.AsNullable());
    }

    private void OnBreakage(Entity<ESDisplayCaseComponent> ent, ref BreakageEventArgs args)
    {
        SetBroken(ent.AsNullable(), true);
    }

    private void OnExamined(Entity<ESDisplayCaseComponent> ent, ref ExaminedEvent args)
    {
        var item = _itemSlots.GetItemOrNull(ent, ent.Comp.ItemSlotId);
        args.PushMarkup(Loc.GetString("es-display-case-examine",
            ("any", item.HasValue),
            ("item", item ?? EntityUid.Invalid)));
    }

    public void SetBroken(Entity<ESDisplayCaseComponent?> ent, bool broken)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.Broken == broken)
            return;

        ent.Comp.Broken = broken;
        Dirty(ent);
        _appearance.SetData(ent, ESDisplayCaseVisuals.Broken, broken);

        UpdateSlotLock(ent);
    }

    public void UpdateSlotLock(Entity<ESDisplayCaseComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        _itemSlots.SetLock(ent, ent.Comp.ItemSlotId, !CanAccess(ent.AsNullable()));
    }

    public bool CanAccess(Entity<ESDisplayCaseComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return true;

        return ent.Comp.Broken;
    }
}
