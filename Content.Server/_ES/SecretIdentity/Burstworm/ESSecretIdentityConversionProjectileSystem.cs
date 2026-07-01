using Content.Server._ES.SecretIdentity.Burstworm.Components;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Shared._ES.Core.Timer;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Robust.Shared.Random;

namespace Content.Server._ES.SecretIdentity.Burstworm;

public sealed partial class ESSecretIdentityConversionProjectileSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ESEntityTimerSystem _entityTimer = default!;
    [Dependency] private ESSecretIdentitySystem _secretIdentity = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private PopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESSecretIdentityConversionProjectileComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ESSecretIdentityConversionProjectileComponent, ESSecretIdentityConversionProjectileTimerEvent>(OnConversionProjectileTimer);
    }

    private void OnMapInit(Entity<ESSecretIdentityConversionProjectileComponent> ent, ref MapInitEvent args)
    {
        _entityTimer.SpawnTimer(ent, ent.Comp.ConvertDelay * _random.NextFloat(1f, 1.5f), new ESSecretIdentityConversionProjectileTimerEvent());
    }

    private void OnConversionProjectileTimer(Entity<ESSecretIdentityConversionProjectileComponent> ent, ref ESSecretIdentityConversionProjectileTimerEvent args)
    {
        if (!TryComp<EmbeddableProjectileComponent>(ent, out var embeddable))
            return;

        if (embeddable.EmbeddedIntoUid is { } embedded &&
            _mind.TryGetMind(embedded, out var mind) &&
            _secretIdentity.GetOrganizationOrNull(mind.Value.AsNullable()) != ent.Comp.IgnoreOrganization)
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.Popup, ("ent", Identity.Entity(embedded, EntityManager))), embedded, PopupType.MediumCaution);
            _secretIdentity.ChangeSecretIdentity(mind.Value, ent.Comp.SecretIdentity);
        }
        else
        {
            SpawnNextToOrDrop(ent.Comp.FailureTrash, ent);
        }

        QueueDel(ent);
    }
}
