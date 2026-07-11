using System.Linq;
using Content.Shared._ES.Chat.Obfuscation.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Alert;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._ES.Disguise;

public sealed partial class ESDisguiseSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private SharedIdCardSystem _idCard = default!;
    [Dependency] private IdentitySystem _identity = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private MaskSystem _mask = default!;

    private readonly Dictionary<(IdentityBlockerCoverage?, bool), ProtoId<AlertPrototype>> _disguiseAlerts = new()
    {
        { (IdentityBlockerCoverage.NONE, false), "ESDisguiseNone" },
        { (IdentityBlockerCoverage.NONE, true), "ESDisguiseNoneVoice" },
        { (IdentityBlockerCoverage.MOUTH, false), "ESDisguiseBottom" },
        { (IdentityBlockerCoverage.MOUTH, true), "ESDisguiseBottomVoice" },
        { (IdentityBlockerCoverage.EYES, false), "ESDisguiseTop" },
        { (IdentityBlockerCoverage.EYES, true), "ESDisguiseTopVoice" },
        { (IdentityBlockerCoverage.FULL, false), "ESDisguiseFull" },
        { (IdentityBlockerCoverage.FULL, true), "ESDisguiseFullVoice" },
        { (null, false), "ESDisguiseID" },
        { (null, true), "ESDisguiseIDVoice" },
    };

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<IdentityComponent, IdentityChangedEvent>(OnIdentityChanged);
    }

    private void OnIdentityChanged(Entity<IdentityComponent> ent, ref IdentityChangedEvent args)
    {
        if (_timing.ApplyingState)
            return;
        RefreshDisguiseAlert(ent.AsNullable());
    }

    public void RefreshDisguiseAlert(Entity<IdentityComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        IdentityBlockerCoverage? coverage = _identity.GetIdentityBlockerCoverage(ent);
        if (coverage == IdentityBlockerCoverage.FULL && _idCard.TryFindIdCard(ent.Owner, out _))
        {
            coverage = null;
        }

        // flakier than i'd like just because this logic is implicitly encoded via events
        var voiceObfuscated = _inventory.GetSlotEntities(ent.Owner, SlotFlags.WITHOUT_POCKET)
            .Any(e => HasComp<ESVoiceObfuscatorComponent>(e) && !_mask.IsToggled(e));

        var disguiseKey = (coverage, voiceObfuscated);
        if (!_disguiseAlerts.TryGetValue(disguiseKey, out var alert))
            return;

        _alerts.ShowAlert(ent.Owner, alert);
    }
}
