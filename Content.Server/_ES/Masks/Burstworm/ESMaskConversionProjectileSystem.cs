using Content.Server._ES.Masks.Burstworm.Components;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Shared._ES.Core.Timer;
using Content.Shared.Popups;
using Content.Shared.Projectiles;

namespace Content.Server._ES.Masks.Burstworm;

public sealed class ESMaskConversionProjectileSystem : EntitySystem
{
    [Dependency] private readonly ESEntityTimerSystem _entityTimer = default!;
    [Dependency] private readonly ESMaskSystem _mask = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESMaskConversionProjectileComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ESMaskConversionProjectileComponent, ESMaskConversionProjectileTimerEvent>(OnConversionProjectileTimer);
    }

    private void OnMapInit(Entity<ESMaskConversionProjectileComponent> ent, ref MapInitEvent args)
    {
        _entityTimer.SpawnTimer(ent, ent.Comp.ConvertDelay, new ESMaskConversionProjectileTimerEvent());
    }

    private void OnConversionProjectileTimer(Entity<ESMaskConversionProjectileComponent> ent, ref ESMaskConversionProjectileTimerEvent args)
    {
        if (!TryComp<EmbeddableProjectileComponent>(ent, out var embeddable))
            return;

        if (embeddable.EmbeddedIntoUid is { } embedded &&
            _mind.TryGetMind(embedded, out var mind) &&
            _mask.GetTroupeOrNull(mind.Value.AsNullable()) != ent.Comp.IgnoreTroupe)
        {
            _mask.ChangeMask(mind.Value, ent.Comp.Mask);
            _popup.PopupEntity(Loc.GetString(ent.Comp.Popup, ("ent", embedded)), embedded, PopupType.MediumCaution);
        }
        else
        {
            SpawnNextToOrDrop(ent.Comp.FailureTrash, ent);
        }

        QueueDel(ent);
    }
}
