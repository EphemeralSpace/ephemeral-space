using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._ES.Masks.Components;
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

namespace Content.Shared._ES.Masks;

public abstract class ESSharedMaskSystem : EntitySystem
{
    [Dependency] protected readonly ISharedAdminManager AdminManager = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] protected readonly SharedMindSystem Mind = default!;
    [Dependency] protected readonly ESSharedObjectiveSystem Objective = default!;
    [Dependency] protected readonly SharedRoleSystem Role = default!;

    protected static readonly VerbCategory ESMask =
        new("es-verb-categories-mask", "/Textures/Interface/emotes.svg.192dpi.png");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetVerbsEvent<Verb>>(GetVerbs);

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

        if (!HasComp<MindContainerComponent>(args.Target) ||
            !TryComp<ActorComponent>(args.Target, out var actorComp))
            return;

        if (_netManager.IsClient)
        {
            args.ExtraCategories.Add(ESMask);
            return;
        }

        var idx = 0;
        var masks = PrototypeManager.EnumeratePrototypes<ESMaskPrototype>()
            .OrderBy(p => Loc.GetString(PrototypeManager.Index(p.Troupe).Name))
            .ThenByDescending(p => Loc.GetString(p.Name));
        foreach (var mask in masks)
        {
            if (mask.Abstract)
                continue;
            var verb = new Verb
            {
                Category = ESMask,
                Text = Loc.GetString("es-verb-apply-mask-name",
                    ("name", Loc.GetString(mask.Name)),
                    ("troupe", Loc.GetString(PrototypeManager.Index(mask.Troupe).Name))),
                Message = Loc.GetString("es-verb-apply-mask-desc", ("mask", Loc.GetString(mask.Name))),
                Priority = idx++,
                ConfirmationPopup = true,
                Act = () =>
                {
                    if (!Mind.TryGetMind(actorComp.PlayerSession, out var mind, out var mindComp))
                        return;
                    ApplyMask((mind, mindComp), mask, null);
                },
            };
            args.Verbs.Add(verb);
        }
    }

    private void OnObjectivesChanged(Entity<ESTroupeRuleComponent> ent, ref ESObjectivesChangedEvent args)
    {
        foreach (var mind in ent.Comp.TroupeMemberMinds)
        {
            Objective.RegenerateObjectiveList(mind);
        }
    }

    private void OnComponentGetStateAttempt(Entity<ESTroupeFactionIconComponent> ent, ref ComponentGetStateAttemptEvent args)
    {
        if (args.Player?.AttachedEntity is not { } attachedEntity)
            return;

        args.Cancelled = GetTroupeOrNull(attachedEntity) != ent.Comp.Troupe;
    }

    private void OnExaminedEvent(Entity<ESTroupeFactionIconComponent> ent, ref ExaminedEvent args)
    {
        // Don't show for yourself
        if (args.Examiner == ent.Owner)
            return;

        if (ent.Comp.ExamineString is not { } str)
            return;

        if (GetTroupeOrNull(args.Examiner) != ent.Comp.Troupe)
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
        args.Objectives.AddRange(Objective.GetObjectives(troupeEntity.Value.Owner));
    }

    /// <summary>
    /// Retrieves the current mask from an entity, failing if they have no mind or mask
    /// </summary>
    public bool TryGetMask(EntityUid uid, [NotNullWhen(true)] out ProtoId<ESMaskPrototype>? mask)
    {
        if (Mind.TryGetMind(uid, out var mindUid, out var mindComp) &&
            TryGetMask((mindUid, mindComp), out mask))
            return true;
        mask = null;
        return false;
    }

    /// <summary>
    /// Retrieves the current mask from a mind, failing if one isn't assigned.
    /// </summary>
    public bool TryGetMask(Entity<MindComponent?> mind, [NotNullWhen(true)] out ProtoId<ESMaskPrototype>? mask)
    {
        mask = null;
        if (!Role.MindHasRole<ESMaskRoleComponent>(mind, out var role))
            return false;

        mask = role.Value.Comp2.Mask;
        return mask != null;
    }

    public ProtoId<ESMaskPrototype>? GetMaskOrNull(EntityUid uid)
    {
        if (!Mind.TryGetMind(uid, out var mindUid, out var mindComp))
            return null;

        return GetMaskOrNull((mindUid, mindComp));
    }

    public ProtoId<ESMaskPrototype>? GetMaskOrNull(Entity<MindComponent?> mind)
    {
        TryGetMask(mind, out var mask);
        return mask;
    }

    /// <summary>
    /// Helper version of <see cref="TryGetMask(Robust.Shared.GameObjects.EntityUid,out Robust.Shared.Prototypes.ProtoId{Content.Shared._ES.Masks.ESMaskPrototype}?)"/> that returns the troupe.
    /// </summary>
    public bool TryGetTroupe(EntityUid uid, [NotNullWhen(true)] out ProtoId<ESTroupePrototype>? troupe)
    {
        troupe = null;
        if (!TryGetMask(uid, out var mask))
            return false;

        troupe = PrototypeManager.Index(mask).Troupe;
        return true;
    }

    /// <summary>
    /// Helper version of <see cref="TryGetMask(Robust.Shared.GameObjects.Entity{Content.Shared.Mind.MindComponent?},out Robust.Shared.Prototypes.ProtoId{Content.Shared._ES.Masks.ESMaskPrototype}?)"/> that returns the troupe.
    /// </summary>
    public bool TryGetTroupe(Entity<MindComponent?> mind, [NotNullWhen(true)] out ProtoId<ESTroupePrototype>? troupe)
    {
        troupe = null;
        if (!TryGetMask(mind, out var mask))
            return false;

        troupe = PrototypeManager.Index(mask).Troupe;
        return true;
    }

    /// <summary>
    /// Variant of <see cref="TryGetTroupe(Robust.Shared.GameObjects.EntityUid,out Robust.Shared.Prototypes.ProtoId{Content.Shared._ES.Masks.ESTroupePrototype}?)"/>
    /// </summary>
    public ProtoId<ESTroupePrototype>? GetTroupeOrNull(EntityUid uid)
    {
        TryGetTroupe(uid, out var troupe);
        return troupe;
    }

    /// <summary>
    /// Variant of <see cref="TryGetTroupe(Robust.Shared.GameObjects.EntityUid,out Robust.Shared.Prototypes.ProtoId{Content.Shared._ES.Masks.ESTroupePrototype}?)"/>
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
            .ThenBy(t => t.Comp.PlayersPerTargetMember)
            .ToList();
    }

    /// <summary>
    ///     Gets the troupe rule for the given mask.
    /// </summary>
    public bool TryGetTroupeEntityForMask(
        ProtoId<ESMaskPrototype> mask,
        [NotNullWhen(true)] out Entity<ESTroupeRuleComponent>? troupe
        )
    {
        return TryGetTroupeEntity(PrototypeManager.Index(mask).Troupe, out troupe);
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
    ///     Applies the given mask to a mind, without any checks.
    /// </summary>
    /// <remarks>
    ///     This allows "bad" game states like giving masks to roles they're incompatible with, and will automatically
    ///     start troupes as necessary.
    /// </remarks>
    public virtual void ApplyMask(Entity<MindComponent> mind,
        ProtoId<ESMaskPrototype> maskId,
        Entity<ESTroupeRuleComponent>? troupe)
    {
        // No Op
    }

    public virtual void RemoveMask(Entity<MindComponent> mind)
    {

    }

    public List<FormattedMessage> GetCharacterInfoBlurb(Entity<MindComponent> mind)
    {
        var ev = new ESGetCharacterInfoBlurbEvent();
        RaiseLocalEvent(mind, ref ev);

        foreach (var role in mind.Comp.MindRoleContainer.ContainedEntities)
        {
            RaiseLocalEvent(role, ref ev);
        }

        return ev.Info;
    }
}

[ByRefEvent]
public record struct ESGetCharacterInfoBlurbEvent()
{
    public List<FormattedMessage> Info = new();
}
