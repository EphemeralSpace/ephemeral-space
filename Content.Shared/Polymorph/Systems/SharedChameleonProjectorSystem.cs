using Content.Shared._ES.Sparks;
using Content.Shared.Coordinates;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Polymorph.Components;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityTable;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;

namespace Content.Shared.Polymorph.Systems;

/// <summary>
/// Handles disguise validation, disguising and revealing.
/// Most appearance copying is done clientside.
/// </summary>
public abstract partial class SharedChameleonProjectorSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private EntityTableSystem _entityTable = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ESSparksSystem _sparks = default!;
    [Dependency] private ItemToggleSystem _toggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChameleonDisguiseComponent, InteractHandEvent>(OnDisguiseInteractHand, before: [typeof(SharedItemSystem)]);
        SubscribeLocalEvent<ChameleonDisguiseComponent, DamageChangedEvent>(OnDisguiseDamaged);
        SubscribeLocalEvent<ChameleonDisguiseComponent, InsertIntoEntityStorageAttemptEvent>(OnDisguiseInsertAttempt);
        SubscribeLocalEvent<ChameleonDisguiseComponent, GettingPickedUpAttemptEvent>(OnDisguisePickedUpAttempt);
        SubscribeLocalEvent<ChameleonDisguisedComponent, EntGotInsertedIntoContainerMessage>(OnDisguisedInserted);

        SubscribeLocalEvent<ChameleonProjectorComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ChameleonProjectorComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
        SubscribeLocalEvent<ChameleonProjectorComponent, HandDeselectedEvent>(OnDeselected);
        SubscribeLocalEvent<ChameleonProjectorComponent, GotUnequippedHandEvent>(OnUnequipped);
        SubscribeLocalEvent<ChameleonProjectorComponent, ComponentShutdown>(OnProjectorShutdown);
        SubscribeLocalEvent<ChameleonProjectorComponent, ItemToggledEvent>(OnProjectorToggled);
    }

    #region Disguise entity

    private void OnDisguiseInteractHand(Entity<ChameleonDisguiseComponent> ent, ref InteractHandEvent args)
    {
        TryReveal(ent.Comp.User);
        args.Handled = true;
    }

    private void OnDisguiseDamaged(Entity<ChameleonDisguiseComponent> ent, ref DamageChangedEvent args)
    {
        // this mirrors damage 1:1
        if (args.DamageDelta is {} damage)
            _damageable.TryChangeDamage(ent.Comp.User, damage);
    }

    private void OnDisguiseInsertAttempt(Entity<ChameleonDisguiseComponent> ent, ref InsertIntoEntityStorageAttemptEvent args)
    {
        // stay parented to the user, not the storage
        args.Cancelled = true;
        TryReveal(ent.Comp.User);
    }

    private void OnDisguisePickedUpAttempt(Entity<ChameleonDisguiseComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        TryReveal(ent.Comp.User);
        args.Cancel();
    }

    #endregion

    #region Disguised player

    private void OnDisguisedInserted(Entity<ChameleonDisguisedComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        // prevent player going into locker/mech/etc while disguised
        TryReveal((ent, ent));
    }

    #endregion

    #region Projector

    private void OnProjectorToggled(Entity<ChameleonProjectorComponent> ent, ref ItemToggledEvent args)
    {
        if (args.Activated)
            return;

        if (ent.Comp.Disguised == null)
            return;

        // We don't toggle here as this is only called when we subscribe to being toggled off.
        TryReveal(ent.Comp.Disguised.Value);
    }

    private void OnUseInHand(Entity<ChameleonProjectorComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.Disguised == null)
        {
            TryDisguise(ent, args.User);
        }
        else
        {
            TryReveal(args.User);
        }
    }

    private void OnGetVerbs(Entity<ChameleonProjectorComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!_hands.IsHolding(args.User, ent))
            return;

        var user = args.User;
        args.Verbs.Add(new InteractionVerb
        {
            Act = () =>
            {
                TryDisguise(ent, user);
            },
            Text = Loc.GetString("chameleon-projector-set-disguise")
        });
    }

    public bool TryDisguise(Entity<ChameleonProjectorComponent> ent, EntityUid user)
    {
        if (_container.IsEntityInContainer(user))
        {
            _popup.PopupEntity(Loc.GetString("chameleon-projector-inside-container"), user, user);
            return false;
        }

        // We do a TryComp, so if the item has variations without ItemToggle, they can still be used just fine.
        if (TryComp<ItemToggleComponent>(ent.Owner, out var itemToggle) && !_toggle.TryActivate((ent.Owner, itemToggle), user))
            return false;

        _popup.PopupEntity(Loc.GetString("chameleon-projector-success"), user, user);
        _sparks.DoSparks(ent, user: user);
        Disguise(ent, user);
        return true;
    }

    private void OnDeselected(Entity<ChameleonProjectorComponent> ent, ref HandDeselectedEvent args)
    {
        RevealProjector(ent);
    }

    private void OnUnequipped(Entity<ChameleonProjectorComponent> ent, ref GotUnequippedHandEvent args)
    {
        RevealProjector(ent);
    }

    private void OnProjectorShutdown(Entity<ChameleonProjectorComponent> ent, ref ComponentShutdown args)
    {
        RevealProjector(ent);
    }

    #endregion

    #region API

    /// <summary>
    /// On server, polymorphs the user into an entity and sets up the disguise.
    /// </summary>
    public void Disguise(Entity<ChameleonProjectorComponent> ent, EntityUid user)
    {
        var proj = ent.Comp;

        // reveal first to allow quick switching
        if (ent.Comp.Disguised != null)
            ClearDisguise(ent, ent.Comp.Disguised.Value);

        proj.Disguised = user;
        Dirty(ent);

        var disguiseProto = _entityTable.GetSingleSpawn(proj.DisguiseProto);
        var disguise = PredictedSpawnAttachedTo(disguiseProto, user.ToCoordinates());

        var disguised = EnsureComp<ChameleonDisguisedComponent>(user);
        disguised.Disguise = disguise;
        Dirty(user, disguised);

        var comp = EnsureComp<ChameleonDisguiseComponent>(disguise);
        comp.User = user;
        comp.Projector = ent;
        Dirty(disguise, comp);
    }

    /// <summary>
    /// Removes the disguise, if the user is disguised.
    /// </summary>
    public bool TryReveal(Entity<ChameleonDisguisedComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (!TryComp<ChameleonDisguiseComponent>(ent.Comp.Disguise, out var disguise)
            || !TryComp<ChameleonProjectorComponent>(disguise.Projector, out var proj))
            return false;

        ClearDisguise((disguise.Projector, proj), ent);
        _toggle.TryDeactivate(disguise.Projector);
        _sparks.DoSparks(ent);

        RemComp<ChameleonDisguisedComponent>(ent);
        return true;
    }

    /// <summary>
    /// Clears the disguise for the projector, allowing the user to immediately disguise again.
    /// </summary>
    /// <param name="ent">The entity for which to clear the disguise</param>
    /// <param name="disguised">The disguised entity.</param>
    private void ClearDisguise(Entity<ChameleonProjectorComponent> ent, Entity<ChameleonDisguisedComponent?> disguised)
    {
        if (!Resolve(disguised, ref disguised.Comp, false))
            return;

        if (ent.Comp.Disguised == null)
            return;

        ent.Comp.Disguised = null;
        Dirty(ent);

        if (!TerminatingOrDeleted(disguised.Comp.Disguise))
            PredictedDel(disguised.Comp.Disguise);
    }

    /// <summary>
    /// Reveal a projector's user, if any.
    /// </summary>
    public void RevealProjector(Entity<ChameleonProjectorComponent> ent)
    {
        if (ent.Comp.Disguised is {} user)
            TryReveal(user);
    }

    #endregion
}
