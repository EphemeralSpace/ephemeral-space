using Content.Shared._ES.Masks.Components;
using Content.Shared.Administration.Managers;
using Content.Shared.Antag;
using Content.Shared.Examine;
using Content.Shared.Verbs;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Masks;

public abstract class ESSharedMaskSystem : EntitySystem
{
    [Dependency] protected readonly ISharedAdminManager AdminManager = default!;
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;

    protected static readonly VerbCategory ESMask =
        new("es-verb-categories-mask", "/Textures/Interface/emotes.svg.192dpi.png");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESTroupeFactionIconComponent, ComponentGetStateAttemptEvent>(OnComponentGetStateAttempt);
        SubscribeLocalEvent<ESTroupeFactionIconComponent, ExaminedEvent>(OnExaminedEvent);
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
}
