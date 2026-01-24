using Content.Shared._ES.Coroner.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Shared._ES.Coroner;

public abstract class ESSharedCoronerSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESCoronerToolComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ESCoronerToolComponent, ESCoronerAnalyzeDoAfterEvent>(OnCoronerAnalyzeDoAfter);
    }

    private void OnAfterInteract(Entity<ESCoronerToolComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target is not { } target)
            return;

        args.Handled = TryUseCoronerTool(ent.AsNullable(), args.User, target);
    }

    private void OnCoronerAnalyzeDoAfter(Entity<ESCoronerToolComponent> ent, ref ESCoronerAnalyzeDoAfterEvent args)
    {
        if (args.Cancelled || args.Target is not { } target)
            return;

        if (!CanUseCoronerTool(ent.AsNullable(), args.User, target))
            return;

        _popup.PopupClient(Loc.GetString("es-coroner-report-complete-popup"), target, args.User, PopupType.Medium);

        // Bitch out while predicting because we can't really do most of this on the client.
        if (_net.IsClient)
        {
            args.Handled = true;
            return;
        }

        var paper = SpawnNextToOrDrop(ent.Comp.ReportPrototype, target);
        _paper.SetContent(paper, GetReport(target).ToMarkup());
        args.Handled = true;
    }

    public bool TryUseCoronerTool(Entity<ESCoronerToolComponent?> tool, EntityUid user, EntityUid target)
    {
        if (!CanUseCoronerTool(tool, user, target))
            return false;

        UseCoronerTool(tool, user, target);
        return true;
    }

    public bool CanUseCoronerTool(Entity<ESCoronerToolComponent?> tool,
        EntityUid user,
        EntityUid target)
    {
        if (!Resolve(tool, ref tool.Comp))
            return false;

        if (!HasComp<ESCoronerUserComponent>(user))
            return false;

        if (!_actionBlocker.CanComplexInteract(user) || !_actionBlocker.CanUseHeldEntity(user, tool))
            return false;

        if (!_mobState.IsDead(target) || !HasComp<HumanoidAppearanceComponent>(target))
            return false;

        return true;
    }

    public void UseCoronerTool(Entity<ESCoronerToolComponent?> tool, EntityUid user, EntityUid target)
    {
        if (!Resolve(tool, ref tool.Comp))
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            user,
            tool.Comp.AnalyzeTime,
            new ESCoronerAnalyzeDoAfterEvent(),
            tool,
            target,
            tool)
        {
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        });
    }

    protected virtual FormattedMessage GetReport(EntityUid target)
    {
        return new FormattedMessage();
    }
}
