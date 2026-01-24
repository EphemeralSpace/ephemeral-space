using Content.Shared._ES.Coroner.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Buckle.Components;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;

namespace Content.Shared._ES.Coroner;

public abstract class ESSharedCoronerSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESAutopsyToolComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ESAutopsyToolComponent, ESAutopsyDoAfterEvent>(OnCoronerAnalyzeDoAfter);
    }

    private void OnAfterInteract(Entity<ESAutopsyToolComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target is not { } target)
            return;

        args.Handled = TryUseCoronerTool(ent.AsNullable(), args.User, target);
    }

    private void OnCoronerAnalyzeDoAfter(Entity<ESAutopsyToolComponent> ent, ref ESAutopsyDoAfterEvent args)
    {
        if (args.Cancelled || args.Target is not { } target)
            return;

        if (!CanUseCoronerTool(ent.AsNullable(), args.User, target, out _))
            return;

        _popup.PopupClient(Loc.GetString("es-coroner-report-complete-popup"), target, args.User, PopupType.Medium);
        _audio.PlayPredicted(ent.Comp.AutopsySound, target, args.User);
        var paper = PredictedSpawnNextToOrDrop(ent.Comp.ReportPrototype, target);
        _paper.SetContent(paper, GetReport(target).ToMarkup());
        args.Handled = true;
    }

    public bool TryUseCoronerTool(Entity<ESAutopsyToolComponent?> tool, EntityUid user, EntityUid target)
    {
        if (!CanUseCoronerTool(tool, user, target, out var reason))
        {
            if (!string.IsNullOrEmpty(reason))
                _popup.PopupClient(reason, target, user, PopupType.SmallCaution);

            return false;
        }

        UseCoronerTool(tool, user, target);
        return true;
    }

    public bool CanUseCoronerTool(Entity<ESAutopsyToolComponent?> tool,
        EntityUid user,
        EntityUid target,
        out string reason)
    {
        reason = string.Empty;

        if (!Resolve(tool, ref tool.Comp))
            return false;

        if (!_actionBlocker.CanComplexInteract(user) || !_actionBlocker.CanUseHeldEntity(user, tool))
            return false;

        if (!HasComp<HumanoidAppearanceComponent>(target))
            return false;

        if (!HasComp<ESAutopsyUserComponent>(user))
        {
            reason = Loc.GetString("es-coroner-autopsy-fail-not-user");
            return false;
        }

        if (!_mobState.IsDead(target))
        {
            reason = Loc.GetString("es-coroner-autopsy-fail-alive");
            return false;
        }

        if (TryComp<BuckleComponent>(target, out var buckle) && !HasComp<ESAutopsyTableComponent>(buckle.BuckledTo))
        {
            reason = Loc.GetString("es-coroner-autopsy-fail-table");
            return false;
        }

        return true;
    }

    public void UseCoronerTool(Entity<ESAutopsyToolComponent?> tool, EntityUid user, EntityUid target)
    {
        if (!Resolve(tool, ref tool.Comp))
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            user,
            tool.Comp.AutopsyTime,
            new ESAutopsyDoAfterEvent(),
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
