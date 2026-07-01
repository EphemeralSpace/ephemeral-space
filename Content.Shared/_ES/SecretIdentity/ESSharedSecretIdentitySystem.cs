using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._ES.SecretIdentity.Components;
using Content.Shared._ES.Objectives;
using Content.Shared._ES.Objectives.Components;
using Content.Shared.Administration;
using Content.Shared.Administration.Managers;
using Content.Shared.Examine;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Verbs;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._ES.SecretIdentity;

public abstract partial class ESSharedSecretIdentitySystem : EntitySystem
{
    [Dependency] protected ISharedAdminManager AdminManager = default!;
    [Dependency] private INetManager _netManager = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] protected IPrototypeManager PrototypeManager = default!;
    [Dependency] protected SharedMindSystem Mind = default!;
    [Dependency] protected ESSharedObjectiveSystem Objective = default!;
    [Dependency] protected SharedRoleSystem Role = default!;

    protected static readonly VerbCategory ESSecretIdentity =
        new("es-verb-categories-secret-identity", "/Textures/Interface/emotes.svg.192dpi.png");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetVerbsEvent<Verb>>(GetVerbs);

        SubscribeLocalEvent<ESSecretIdentityRoleComponent, MindGotAddedEvent>(OnSecretIdentityRoleGotAdded);

        SubscribeLocalEvent<ESTroupeRuleComponent, ESObjectivesChangedEvent>(OnObjectivesChanged);

        SubscribeLocalEvent<ESTroupeFactionIconComponent, ComponentGetStateAttemptEvent>(OnComponentGetStateAttempt);
        SubscribeLocalEvent<ESTroupeFactionIconComponent, ExaminedEvent>(OnExaminedEvent);
        SubscribeLocalEvent<ESTroupeFactionIconComponent, ComponentStartup>(OnFactionIconStartup);

        SubscribeLocalEvent<MindComponent, ESGetAdditionalObjectivesEvent>(OnMindGetObjectives);
    }

    private void GetVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        var player = actor.PlayerSession;

        if (!AdminManager.HasAdminFlag(player, AdminFlags.Fun))
            return;

        if (!Mind.TryGetMind(args.Target, out var mind))
            return;

        if (_netManager.IsClient)
        {
            args.ExtraCategories.Add(ESSecretIdentity);
            return;
        }

        var idx = 0;
        var secretIdentities = PrototypeManager.EnumeratePrototypes<ESSecretIdentityPrototype>()
            .OrderBy(p => Loc.GetString(PrototypeManager.Index(p.Troupe).Name))
            .ThenByDescending(p => Loc.GetString(p.Name));
        foreach (var secretIdentity in secretIdentities)
        {
            if (secretIdentity.Abstract)
                continue;

            var troupe = PrototypeManager.Index(secretIdentity.Troupe);

            var verb = new Verb
            {
                Category = ESSecretIdentity,
                Icon = PrototypeManager.Index(troupe.MetaIcon).Icon,
                Text = Loc.GetString("es-verb-apply-secret-identity-name",
                    ("name", Loc.GetString(secretIdentity.Name)),
                    ("color", secretIdentity.Color)),
                Message = Loc.GetString("es-verb-apply-secret-identity-desc",
                    ("secretIdentity", Loc.GetString(secretIdentity.Name)),
                    ("troupe", Loc.GetString(troupe.Name))),
                Priority = idx++,
                ConfirmationPopup = true,
                Act = () =>
                {
                    ChangeSecretIdentity(mind.Value, secretIdentity, eraseHistory: true);
                },
            };
            args.Verbs.Add(verb);
        }
    }

    private void OnSecretIdentityRoleGotAdded(Entity<ESSecretIdentityRoleComponent> ent, ref MindGotAddedEvent args)
    {
        if (!ent.Comp.SecretIdentity.HasValue)
            return;
        EnsureComp<ESBodyLastSecretIdentityComponent>(args.Container).LastSecretIdentity = ent.Comp.SecretIdentity.Value;
    }

    private void OnObjectivesChanged(Entity<ESTroupeRuleComponent> ent, ref ESObjectivesChangedEvent args)
    {
        foreach (var mind in ent.Comp.TroupeMemberMinds)
        {
            Objective.RegenerateObjectiveList(mind);
        }
    }

    private bool CanShowFactionIcons(Entity<ESTroupeFactionIconComponent> ent, EntityUid viewer)
    {
        var troupe = GetTroupeOrNull(viewer);
        var mind = Mind.GetMind(viewer);
        var ignored = TryComp<ESTroupeIgnoreFactionIconsComponent>(mind, out var ignoreIcons) &&
                      ignoreIcons.Troupes.Contains(ent.Comp.Troupe);
        return troupe == ent.Comp.Troupe && !ignored;
    }

    private void OnComponentGetStateAttempt(Entity<ESTroupeFactionIconComponent> ent, ref ComponentGetStateAttemptEvent args)
    {
        if (args.Player?.AttachedEntity is not { } attachedEntity)
            return;

        args.Cancelled = !CanShowFactionIcons(ent, attachedEntity);
    }

    private void OnExaminedEvent(Entity<ESTroupeFactionIconComponent> ent, ref ExaminedEvent args)
    {
        // Don't show for yourself
        if (args.Examiner == ent.Owner)
            return;

        if (ent.Comp.ExamineString is not { } str)
            return;

        if (!CanShowFactionIcons(ent, args.Examiner))
            return;

        args.PushMarkup(Loc.GetString(str));
    }

    private void OnFactionIconStartup(Entity<ESTroupeFactionIconComponent> ent, ref ComponentStartup args)
    {
        // When someone receives this component, we need to essentially refresh all other instances of faction icons
        // so that they can see the icons of all other players. The only way to do this is apparently just dirtying every
        // instance of the component, which sucks and is terrible. But so is this entire API so i don't give a shit.

        // This logic is based on the similar implementation in SharedRevolutionarySystem so i'll just assume it's correct.

        var query = EntityQueryEnumerator<ESTroupeFactionIconComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var comp, out var meta))
        {
            // THANK YOU
            // THANK YOU
            // THANK YOU
            Dirty(uid, comp, meta);
        }
    }

    private void OnMindGetObjectives(Entity<MindComponent> ent, ref ESGetAdditionalObjectivesEvent args)
    {
        if (!TryGetTroupe(ent.AsNullable(), out var troupe) ||
            !TryGetTroupeEntity(troupe.Value, out var troupeEntity))
            return;

        if (TryComp<ESTroupeNoSharedObjectivesComponent>(ent, out var noObjectives)
            && noObjectives.Troupes.Contains(troupe.Value))
            return;

        args.Objectives.AddRange(Objective.GetObjectives(troupeEntity.Value.Owner));
    }

    /// <summary>
    /// Retrieves the current secret identity from an entity, failing if they have no mind or secret identity
    /// </summary>
    public bool TryGetSecretIdentity(EntityUid uid, [NotNullWhen(true)] out ProtoId<ESSecretIdentityPrototype>? secretIdentity)
    {
        if (Mind.TryGetMind(uid, out var mindUid, out var mindComp) &&
            TryGetSecretIdentity((mindUid, mindComp), out secretIdentity))
            return true;
        secretIdentity = null;
        return false;
    }

    /// <summary>
    /// Retrieves the current secret identity from a mind, failing if one isn't assigned.
    /// </summary>
    public bool TryGetSecretIdentity(Entity<MindComponent?> mind, [NotNullWhen(true)] out ProtoId<ESSecretIdentityPrototype>? secretIdentity)
    {
        secretIdentity = null;
        if (!Role.MindHasRole<ESSecretIdentityRoleComponent>(mind, out var role))
            return false;

        secretIdentity = role.Value.Comp2.SecretIdentity;
        return secretIdentity != null;
    }

    public ProtoId<ESSecretIdentityPrototype>? GetSecretIdentityOrNull(EntityUid uid)
    {
        if (!Mind.TryGetMind(uid, out var mindUid, out var mindComp))
            return null;

        return GetSecretIdentityOrNull((mindUid, mindComp));
    }

    public ProtoId<ESSecretIdentityPrototype>? GetSecretIdentityOrNull(Entity<MindComponent?> mind)
    {
        TryGetSecretIdentity(mind, out var secretIdentity);
        return secretIdentity;
    }

    /// <summary>
    /// Helper version of <see cref="TryGetSecretIdentity(Robust.Shared.GameObjects.EntityUid,out Robust.Shared.Prototypes.ProtoId{Content.Shared._ES.SecretIdentity.ESSecretIdentityPrototype}?)"/> that returns the troupe.
    /// </summary>
    public bool TryGetTroupe(EntityUid uid, [NotNullWhen(true)] out ProtoId<ESTroupePrototype>? troupe)
    {
        troupe = null;
        if (!TryGetSecretIdentity(uid, out var secretIdentity))
            return false;

        troupe = PrototypeManager.Index(secretIdentity).Troupe;
        return true;
    }

    /// <summary>
    /// Helper version of <see cref="TryGetSecretIdentity(Robust.Shared.GameObjects.Entity{Content.Shared.Mind.MindComponent?},out Robust.Shared.Prototypes.ProtoId{Content.Shared._ES.SecretIdentity.ESSecretIdentityPrototype}?)"/> that returns the troupe.
    /// </summary>
    public bool TryGetTroupe(Entity<MindComponent?> mind, [NotNullWhen(true)] out ProtoId<ESTroupePrototype>? troupe)
    {
        troupe = null;
        if (!TryGetSecretIdentity(mind, out var secretIdentity))
            return false;

        troupe = PrototypeManager.Index(secretIdentity).Troupe;
        return true;
    }

    /// <summary>
    /// Variant of <see cref="TryGetTroupe(Robust.Shared.GameObjects.EntityUid,out Robust.Shared.Prototypes.ProtoId{Content.Shared._ES.SecretIdentity.ESTroupePrototype}?)"/>
    /// </summary>
    public ProtoId<ESTroupePrototype>? GetTroupeOrNull(EntityUid uid)
    {
        TryGetTroupe(uid, out var troupe);
        return troupe;
    }

    /// <summary>
    /// Variant of <see cref="TryGetTroupe(Robust.Shared.GameObjects.EntityUid,out Robust.Shared.Prototypes.ProtoId{Content.Shared._ES.SecretIdentity.ESTroupePrototype}?)"/>
    /// </summary>
    public ProtoId<ESTroupePrototype>? GetTroupeOrNull(Entity<MindComponent?> mind)
    {
        TryGetTroupe(mind, out var troupe);
        return troupe;
    }

    public List<Entity<ESTroupeRuleComponent>> GetOrderedTroupes()
    {
        var troupes = new List<Entity<ESTroupeRuleComponent>>();
        var query = EntityQueryEnumerator<ESTroupeRuleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            troupes.Add((uid, comp));
        }

        return troupes
            .OrderBy(t => t.Comp.Priority)
            .ToList();
    }

    /// <summary>
    ///     Gets the troupe rule for the given secret identity.
    /// </summary>
    public bool TryGetTroupeEntityForSecretIdentity(
        ProtoId<ESSecretIdentityPrototype> secretIdentity,
        [NotNullWhen(true)] out Entity<ESTroupeRuleComponent>? troupe
        )
    {
        return TryGetTroupeEntity(PrototypeManager.Index(secretIdentity).Troupe, out troupe);
    }

    public bool TryGetTroupeEntity(ProtoId<ESTroupePrototype> proto,
        [NotNullWhen(true)] out Entity<ESTroupeRuleComponent>? troupe)
    {
        troupe = null;
        var query = EntityQueryEnumerator<ESTroupeRuleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Troupe != proto)
                continue;
            troupe = (uid, comp);
            break;
        }

        return troupe != null;
    }

    /// <summary>
    ///     Applies the given secret identity to a mind, without any checks.
    /// </summary>
    /// <remarks>
    ///     This allows "bad" game states like giving secret identities to roles they're incompatible with, and will automatically
    ///     start troupes as necessary.
    /// </remarks>
    public virtual void ApplySecretIdentity(Entity<MindComponent> mind,
        ProtoId<ESSecretIdentityPrototype> secretIdentityId,
        Entity<ESTroupeRuleComponent>? troupe = null)
    {
        // No Op
    }

    public virtual void ChangeSecretIdentity(Entity<MindComponent> mind,
        ProtoId<ESSecretIdentityPrototype> secretIdentityId,
        Entity<ESTroupeRuleComponent>? troupe = null,
        bool eraseHistory = false)
    {

    }

    public virtual void RemoveSecretIdentity(Entity<MindComponent> mind)
    {

    }

    /// <inheritdoc cref="GetTroupeMembers(ProtoId{ESTroupePrototype})"/>
    public IEnumerable<EntityUid> GetTroupeMembers(Entity<ESTroupeRuleComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return [];

        return GetTroupeMembers(ent.Comp.Troupe);
    }

    /// <summary>
    /// Returns all minds who are members of a given troupe.
    /// </summary>
    public IEnumerable<EntityUid> GetTroupeMembers(ProtoId<ESTroupePrototype> troupe)
    {
        if (!TryGetTroupeEntity(troupe, out var troupeEnt))
            yield break;

        foreach (var mind in troupeEnt.Value.Comp.TroupeMemberMinds)
        {
            yield return mind;
        }
    }

    /// <summary>
    /// Returns all minds nearby who are members of a given hostile troupe
    /// </summary>
    public IEnumerable<EntityUid> GetNearbyHostileTroupeMembers(Entity<ESHostileTowardsTroupeComponent?> ent, float range)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            yield break;

        var xform = Transform(ent);

        foreach (var entity in _lookup.GetEntitiesInRange<ESBodyLastSecretIdentityComponent>(_xform.GetMapCoordinates(ent, xform), range))
        {
            var secretIdentity = PrototypeManager.Index(entity.Comp.LastSecretIdentity);
            var troupe = secretIdentity.Troupe;

            if (ent.Comp.NonHostileTroupes != null && ent.Comp.NonHostileTroupes.Contains(troupe))
                continue;

            if (ent.Comp.HostileTroupes != null && !ent.Comp.HostileTroupes.Contains(troupe))
                continue;

            yield return entity.Owner;
        }
    }

    /// <inheritdoc cref="GetNotTroupeMembers(ProtoId{ESTroupePrototype})"/>
    public IEnumerable<EntityUid> GetNotTroupeMembers(Entity<ESTroupeRuleComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return [];

        return GetNotTroupeMembers(ent.Comp.Troupe);
    }

    /// <summary>
    /// Returns all minds who are members of a troupe that is NOT the specified troupe.
    /// Set difference between all player minds and <see cref="GetTroupeMembers(ProtoId{ESTroupePrototype})"/>
    /// </summary>
    public IEnumerable<EntityUid> GetNotTroupeMembers(ProtoId<ESTroupePrototype> troupe)
    {
        foreach (var troupeEnt in GetOrderedTroupes())
        {
            if (troupeEnt.Comp.Troupe == troupe)
                continue;

            foreach (var mind in troupeEnt.Comp.TroupeMemberMinds)
            {
                yield return mind;
            }
        }
    }

    public void RefreshCharacterInfoBlurb(Entity<MindComponent?> mind)
    {
        if (!Resolve(mind, ref mind.Comp))
            return;

        var ev = new ESGetCharacterInfoBlurbEvent();
        RaiseLocalEvent(mind, ref ev);

        foreach (var role in mind.Comp.MindRoleContainer.ContainedEntities)
        {
            RaiseLocalEvent(role, ref ev);
        }

        var comp = EnsureComp<ESCharacterBlurbComponent>(mind);
        comp.Info = new(ev.Info);
        Dirty(mind, comp);
    }

    public List<FormattedMessage> GetCharacterInfoBlurb(Entity<ESCharacterBlurbComponent?> mind)
    {
        if (!Resolve(mind, ref mind.Comp, false))
            return [];

        return mind.Comp.Info;
    }
}

[ByRefEvent]
public record struct ESGetCharacterInfoBlurbEvent()
{
    public List<FormattedMessage> Info = new();
}
