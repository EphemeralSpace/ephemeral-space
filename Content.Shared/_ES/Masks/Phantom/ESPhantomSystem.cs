using System.Linq;
using Content.Shared._ES.Interaction.HoldToFace;
using Content.Shared._ES.Masks.Phantom.Components;
using Content.Shared.Actions;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._ES.Masks.Phantom;

public sealed class ESPhantomSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESPhantomComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ESPhantomComponent, ESPhantomMaterializeActionEvent>(OnMaterializeAction);

        SubscribeLocalEvent<ESPhantomMaterializedComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<ESPhantomMaterializedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ESPhantomMaterializedComponent, ComponentRemove>(OnRemove);
    }

    private void OnMapInit(Entity<ESPhantomComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.MaterializeAction);
        Dirty(ent);
    }

    private void OnMaterializeAction(Entity<ESPhantomComponent> ent, ref ESPhantomMaterializeActionEvent args)
    {
        if (_physics.GetEntitiesIntersectingBody(ent, (int) CollisionGroup.Impassable).Count > 0)
        {
            _popup.PopupPredicted(Loc.GetString("es-phantom-materialize-fail"), ent, ent, PopupType.Medium);
            return;
        }

        EnsureComp<ESPhantomMaterializedComponent>(ent);
        args.Handled = true;
    }

    private void OnDamageChanged(Entity<ESPhantomMaterializedComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;
        RemCompDeferred<ESPhantomMaterializedComponent>(ent);
    }

    private void OnStartup(Entity<ESPhantomMaterializedComponent> ent, ref ComponentStartup args)
    {
        _appearance.SetData(ent, ESPhantomVisuals.Materialized, true);

        if (TryComp<ESPhantomComponent>(ent, out var comp))
        {
            _actions.SetEnabled(comp.ActionEntity, false);
        }

        if (TryComp<FixturesComponent>(ent, out var fixtures) && fixtures.FixtureCount >= 1)
        {
            var fixture = fixtures.Fixtures.First();

            _physics.SetCollisionMask(ent, fixture.Key, fixture.Value, (int) (CollisionGroup.SmallMobMask | CollisionGroup.GhostImpassable), fixtures);
            _physics.SetCollisionLayer(ent, fixture.Key, fixture.Value, (int) CollisionGroup.SmallMobLayer, fixtures);
        }

        RemComp<ESBlockInteractionComponent>(ent);
    }

    private void OnRemove(Entity<ESPhantomMaterializedComponent> ent, ref ComponentRemove args)
    {
        _appearance.SetData(ent, ESPhantomVisuals.Materialized, false);

        if (TryComp<ESPhantomComponent>(ent, out var comp))
        {
            _actions.SetEnabled(comp.ActionEntity, true);
            _actions.SetCooldown(comp.ActionEntity, comp.Cooldown);
        }

        if (TryComp<FixturesComponent>(ent, out var fixtures) && fixtures.FixtureCount >= 1)
        {
            var fixture = fixtures.Fixtures.First();

            _physics.SetCollisionMask(ent, fixture.Key, fixture.Value, (int) CollisionGroup.GhostImpassable, fixtures);
            _physics.SetCollisionLayer(ent, fixture.Key, fixture.Value, 0, fixtures);
        }

        // Drop anything picked up
        foreach (var hand in _hands.EnumerateHands(ent.Owner))
        {
            _hands.DoDrop(ent.Owner, hand);
        }

        EnsureComp<ESBlockInteractionComponent>(ent);
    }
}
