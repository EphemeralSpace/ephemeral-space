using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._ES.Masks.Components;
using Content.Shared.Administration;
using Content.Shared.Administration.Managers;
using Content.Shared.Antag;
using Content.Shared.Examine;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Verbs;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Masks;

public abstract class ESSharedMaskSystem : EntitySystem
{
    [Dependency] protected readonly ISharedAdminManager AdminManager = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] protected readonly SharedMindSystem Mind = default!;
    [Dependency] protected readonly SharedRoleSystem Role = default!;

    protected static readonly VerbCategory ESMask =
        new("es-verb-categories-mask", "/Textures/Interface/emotes.svg.192dpi.png");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetVerbsEvent<Verb>>(GetVerbs);

        SubscribeLocalEvent<ESTroupeFactionIconComponent, ComponentGetStateAttemptEvent>(OnComponentGetStateAttempt);
        SubscribeLocalEvent<ESTroupeFactionIconComponent, ExaminedEvent>(OnExaminedEvent);
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

            TryGetTroupeEntity(mask.Troupe, out var troupe);

            var verb = new Verb
            {
                Category = ESMask,
                Text = Loc.GetString("es-verb-apply-mask-name",
                    ("name", Loc.GetString(mask.Name)),
                    ("troupe", Loc.GetString(PrototypeManager.Index(mask.Troupe).Name))),
                Message = troupe.HasValue
                    ? Loc.GetString("es-verb-apply-mask-desc", ("mask", Loc.GetString(mask.Name)))
                    : Loc.GetString("es-verb-tooltip-no-troupe", ("troupe", Loc.GetString(PrototypeManager.Index(mask.Troupe).Name))),
                Priority = idx++,
                ConfirmationPopup = true,
                Disabled = !troupe.HasValue,
                Act = () =>
                {
                    if (troupe is null)
                        return;
                    if (!Mind.TryGetMind(actorComp.PlayerSession, out var mind, out var mindComp))
                        return;
                    ApplyMask((mind, mindComp), mask, troupe.Value);
                },
            };
            args.Verbs.Add(verb);
        }
    }

    private void OnComponentGetStateAttempt(Entity<ESTroupeFactionIconComponent> ent, ref ComponentGetStateAttemptEvent args)
    {
        args.Cancelled = true;

        if (args.Player?.AttachedEntity is not { } attachedEntity)
            return;

        if (HasComp<ShowAntagIconsComponent>(attachedEntity))
        {
            args.Cancelled = false;
            return;
        }

        if (!TryComp<ESTroupeFactionIconComponent>(attachedEntity, out var component))
            return;
        if (ent.Comp.Troupe != component.Troupe)
            return;
        args.Cancelled = false;
    }

    private void OnExaminedEvent(Entity<ESTroupeFactionIconComponent> ent, ref ExaminedEvent args)
    {
        // Don't show for yourself
        if (args.Examiner == ent.Owner)
            return;

        if (ent.Comp.ExamineString is not { } str)
            return;

        if (!TryComp<ESTroupeFactionIconComponent>(args.Examiner, out var component) ||
            component.Troupe != ent.Comp.Troupe)
            return;

        args.PushMarkup(Loc.GetString(str));
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

    public virtual void ApplyMask(Entity<MindComponent> mind,
        ProtoId<ESMaskPrototype> maskId,
        Entity<ESTroupeRuleComponent> troupe)
    {
        // No Op
    }
}
