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

        SubscribeLocalEvent<ESOrganizationRuleComponent, ESObjectivesChangedEvent>(OnObjectivesChanged);

        SubscribeLocalEvent<ESOrganizationFactionIconComponent, ComponentGetStateAttemptEvent>(OnComponentGetStateAttempt);
        SubscribeLocalEvent<ESOrganizationFactionIconComponent, ExaminedEvent>(OnExaminedEvent);
        SubscribeLocalEvent<ESOrganizationFactionIconComponent, ComponentStartup>(OnFactionIconStartup);

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
            .OrderBy(p => Loc.GetString(PrototypeManager.Index(p.Organization).Name))
            .ThenByDescending(p => Loc.GetString(p.Name));
        foreach (var secretIdentity in secretIdentities)
        {
            if (secretIdentity.Abstract)
                continue;

            var organization = PrototypeManager.Index(secretIdentity.Organization);

            var verb = new Verb
            {
                Category = ESSecretIdentity,
                Icon = PrototypeManager.Index(organization.MetaIcon).Icon,
                Text = Loc.GetString("es-verb-apply-secret-identity-name",
                    ("name", Loc.GetString(secretIdentity.Name)),
                    ("color", secretIdentity.Color)),
                Message = Loc.GetString("es-verb-apply-secret-identity-desc",
                    ("secretIdentity", Loc.GetString(secretIdentity.Name)),
                    ("organization", Loc.GetString(organization.Name))),
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

    private void OnObjectivesChanged(Entity<ESOrganizationRuleComponent> ent, ref ESObjectivesChangedEvent args)
    {
        foreach (var mind in ent.Comp.OrganizationMemberMinds)
        {
            Objective.RegenerateObjectiveList(mind);
        }
    }

    private bool CanShowFactionIcons(Entity<ESOrganizationFactionIconComponent> ent, EntityUid viewer)
    {
        var organization = GetOrganizationOrNull(viewer);
        var mind = Mind.GetMind(viewer);
        var ignored = TryComp<ESOrganizationIgnoreFactionIconsComponent>(mind, out var ignoreIcons) &&
                      ignoreIcons.Organizations.Contains(ent.Comp.Organization);
        return organization == ent.Comp.Organization && !ignored;
    }

    private void OnComponentGetStateAttempt(Entity<ESOrganizationFactionIconComponent> ent, ref ComponentGetStateAttemptEvent args)
    {
        if (args.Player?.AttachedEntity is not { } attachedEntity)
            return;

        args.Cancelled = !CanShowFactionIcons(ent, attachedEntity);
    }

    private void OnExaminedEvent(Entity<ESOrganizationFactionIconComponent> ent, ref ExaminedEvent args)
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

    private void OnFactionIconStartup(Entity<ESOrganizationFactionIconComponent> ent, ref ComponentStartup args)
    {
        // When someone receives this component, we need to essentially refresh all other instances of faction icons
        // so that they can see the icons of all other players. The only way to do this is apparently just dirtying every
        // instance of the component, which sucks and is terrible. But so is this entire API so i don't give a shit.

        // This logic is based on the similar implementation in SharedRevolutionarySystem so i'll just assume it's correct.

        var query = EntityQueryEnumerator<ESOrganizationFactionIconComponent, MetaDataComponent>();
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
        if (!TryGetOrganization(ent.AsNullable(), out var organization) ||
            !TryGetOrganizationEntity(organization.Value, out var organizationEntity))
            return;

        if (TryComp<ESOrganizationNoSharedObjectivesComponent>(ent, out var noObjectives)
            && noObjectives.Organizations.Contains(organization.Value))
            return;

        args.Objectives.AddRange(Objective.GetObjectives(organizationEntity.Value.Owner));
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
    /// Variant of <see cref="TryGetSecretIdentity(EntityUid, out ProtoId{ESSecretIdentityPrototype}?)"/> that does not involve the mind.
    /// So in situations like a phantom where the mind has left the body, this will still return the correct result.
    /// </summary>
    /// <remarks>
    /// Will never return a secret identity for the client.
    /// </remarks>
    public bool TryGetLastSecretIdentity(Entity<ESBodyLastSecretIdentityComponent?> ent, [NotNullWhen(true)] out ProtoId<ESSecretIdentityPrototype>? prototype)
    {
        prototype = null;
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        prototype = ent.Comp.LastSecretIdentity;
        return prototype is not null;
    }

    /// <summary>
    /// Helper version of <see cref="TryGetSecretIdentity(Robust.Shared.GameObjects.EntityUid,out Robust.Shared.Prototypes.ProtoId{Content.Shared._ES.SecretIdentity.ESSecretIdentityPrototype}?)"/> that returns the organization.
    /// </summary>
    public bool TryGetOrganization(EntityUid uid, [NotNullWhen(true)] out ProtoId<ESOrganizationPrototype>? organization)
    {
        organization = null;
        if (!TryGetSecretIdentity(uid, out var secretIdentity))
            return false;

        organization = PrototypeManager.Index(secretIdentity).Organization;
        return true;
    }

    /// <summary>
    /// Helper version of <see cref="TryGetSecretIdentity(Robust.Shared.GameObjects.Entity{Content.Shared.Mind.MindComponent?},out Robust.Shared.Prototypes.ProtoId{Content.Shared._ES.SecretIdentity.ESSecretIdentityPrototype}?)"/> that returns the organization.
    /// </summary>
    public bool TryGetOrganization(Entity<MindComponent?> mind, [NotNullWhen(true)] out ProtoId<ESOrganizationPrototype>? organization)
    {
        organization = null;
        if (!TryGetSecretIdentity(mind, out var secretIdentity))
            return false;

        organization = PrototypeManager.Index(secretIdentity).Organization;
        return true;
    }

    /// <summary>
    /// Variant of <see cref="TryGetOrganization(Robust.Shared.GameObjects.EntityUid,out Robust.Shared.Prototypes.ProtoId{Content.Shared._ES.SecretIdentity.ESOrganizationPrototype}?)"/>
    /// </summary>
    public ProtoId<ESOrganizationPrototype>? GetOrganizationOrNull(EntityUid uid)
    {
        TryGetOrganization(uid, out var organization);
        return organization;
    }

    /// <summary>
    /// Variant of <see cref="TryGetOrganization(Robust.Shared.GameObjects.EntityUid,out Robust.Shared.Prototypes.ProtoId{Content.Shared._ES.SecretIdentity.ESOrganizationPrototype}?)"/>
    /// </summary>
    public ProtoId<ESOrganizationPrototype>? GetOrganizationOrNull(Entity<MindComponent?> mind)
    {
        TryGetOrganization(mind, out var organization);
        return organization;
    }

    public List<Entity<ESOrganizationRuleComponent>> GetOrderedOrganizations()
    {
        var organizations = new List<Entity<ESOrganizationRuleComponent>>();
        var query = EntityQueryEnumerator<ESOrganizationRuleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            organizations.Add((uid, comp));
        }

        return organizations
            .OrderBy(t => t.Comp.Priority)
            .ToList();
    }

    /// <summary>
    ///     Gets the organization rule for the given secret identity.
    /// </summary>
    public bool TryGetOrganizationEntityForSecretIdentity(
        ProtoId<ESSecretIdentityPrototype> secretIdentity,
        [NotNullWhen(true)] out Entity<ESOrganizationRuleComponent>? organization
        )
    {
        return TryGetOrganizationEntity(PrototypeManager.Index(secretIdentity).Organization, out organization);
    }

    public bool TryGetOrganizationEntity(ProtoId<ESOrganizationPrototype> proto,
        [NotNullWhen(true)] out Entity<ESOrganizationRuleComponent>? organization)
    {
        organization = null;
        var query = EntityQueryEnumerator<ESOrganizationRuleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Organization != proto)
                continue;
            organization = (uid, comp);
            break;
        }

        return organization != null;
    }

    /// <summary>
    ///     Applies the given secret identity to a mind, without any checks.
    /// </summary>
    /// <remarks>
    ///     This allows "bad" game states like giving secret identities to roles they're incompatible with, and will automatically
    ///     start organizations as necessary.
    /// </remarks>
    public virtual void ApplySecretIdentity(Entity<MindComponent> mind,
        ProtoId<ESSecretIdentityPrototype> secretIdentityId,
        Entity<ESOrganizationRuleComponent>? organization = null)
    {
        // No Op
    }

    public virtual void ChangeSecretIdentity(Entity<MindComponent> mind,
        ProtoId<ESSecretIdentityPrototype> secretIdentityId,
        Entity<ESOrganizationRuleComponent>? organization = null,
        bool eraseHistory = false)
    {

    }

    public virtual void RemoveSecretIdentity(Entity<MindComponent> mind)
    {

    }

    /// <inheritdoc cref="GetOrganizationMembers(ProtoId{ESOrganizationPrototype})"/>
    public IEnumerable<EntityUid> GetOrganizationMembers(Entity<ESOrganizationRuleComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return [];

        return GetOrganizationMembers(ent.Comp.Organization);
    }

    /// <summary>
    /// Returns all minds who are members of a given organization.
    /// </summary>
    public IEnumerable<EntityUid> GetOrganizationMembers(ProtoId<ESOrganizationPrototype> organization)
    {
        if (!TryGetOrganizationEntity(organization, out var organizationEnt))
            yield break;

        foreach (var mind in organizationEnt.Value.Comp.OrganizationMemberMinds)
        {
            yield return mind;
        }
    }

    /// <summary>
    /// Returns all minds nearby who are members of a given hostile organization
    /// </summary>
    public IEnumerable<EntityUid> GetNearbyHostileOrganizationMembers(Entity<ESHostileTowardsOrganizationComponent?> ent, float range)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            yield break;

        var xform = Transform(ent);

        foreach (var entity in _lookup.GetEntitiesInRange<ESBodyLastSecretIdentityComponent>(_xform.GetMapCoordinates(ent, xform), range))
        {
            if (!TryGetLastSecretIdentity(entity.AsNullable(), out var secretIdentityId))
                continue;

            var secretIdentity = PrototypeManager.Index(secretIdentityId);
            var organization = secretIdentity.Organization;

            if (ent.Comp.NonHostileOrganizations != null && ent.Comp.NonHostileOrganizations.Contains(organization))
                continue;

            if (ent.Comp.HostileOrganizations != null && !ent.Comp.HostileOrganizations.Contains(organization))
                continue;

            yield return entity.Owner;
        }
    }

    /// <inheritdoc cref="GetNotOrganizationMembers(ProtoId{ESOrganizationPrototype})"/>
    public IEnumerable<EntityUid> GetNotOrganizationMembers(Entity<ESOrganizationRuleComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return [];

        return GetNotOrganizationMembers(ent.Comp.Organization);
    }

    /// <summary>
    /// Returns all minds who are members of a organization that is NOT the specified organization.
    /// Set difference between all player minds and <see cref="GetOrganizationMembers(ProtoId{ESOrganizationPrototype})"/>
    /// </summary>
    public IEnumerable<EntityUid> GetNotOrganizationMembers(ProtoId<ESOrganizationPrototype> organization)
    {
        foreach (var organizationEnt in GetOrderedOrganizations())
        {
            if (organizationEnt.Comp.Organization == organization)
                continue;

            foreach (var mind in organizationEnt.Comp.OrganizationMemberMinds)
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
