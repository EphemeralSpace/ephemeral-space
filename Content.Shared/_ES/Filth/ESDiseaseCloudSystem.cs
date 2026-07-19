using Content.Shared._ES.Filth.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Medical;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;

namespace Content.Shared._ES.Filth;

public sealed partial class ESDiseaseCloudSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private VomitSystem _vomit = default!;

    // TODO: make this the disease susceptible component once we support that
    [Dependency] private EntityQuery<MobStateComponent> _mobStateQuery;

    private readonly List<string> _protectionSlots = new() { "head", "outerClothing" };

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESDiseaseCloudComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<ESDiseaseCloudProtectionComponent, ExaminedEvent>(OnExamine);
    }

    private void OnStartCollide(Entity<ESDiseaseCloudComponent> ent, ref StartCollideEvent args)
    {
        if (_net.IsClient)
            return;

        if (!args.OtherFixture.Hard || !_mobStateQuery.TryComp(args.OtherEntity, out var mob) || !_mobState.IsAlive(args.OtherEntity, mob))
            return;

        if (TerminatingOrDeleted(ent) || EntityManager.IsQueuedForDeletion(ent))
            return;

        if (!HasProtection(args.OtherEntity))
        {
            _damageable.TryChangeDamage(args.OtherEntity, ent.Comp.DiseaseDamage, true);
            _vomit.Vomit(args.OtherEntity);
            _popup.PopupEntity(Loc.GetString("es-disease-cloud-hit", ("entity", args.OtherEntity)), args.OtherEntity, PopupType.MediumCaution);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("es-disease-cloud-block"), args.OtherEntity, PopupType.Medium);
        }

        _audio.PlayPvs(ent.Comp.DiseaseSound, args.OtherEntity);

        PredictedQueueDel(ent);
    }

    private void OnExamine(Entity<ESDiseaseCloudProtectionComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("es-disease-cloud-protection-examine"));
    }

    private bool HasProtection(EntityUid uid)
    {
        var hasProtection = false;
        foreach (var slot in _protectionSlots)
        {
            if (!_inventory.TryGetSlotEntity(uid, slot, out var equipment) ||
                !HasComp<ESDiseaseCloudProtectionComponent>(equipment))
                return false;
            hasProtection = true;
        }

        return hasProtection;
    }
}
