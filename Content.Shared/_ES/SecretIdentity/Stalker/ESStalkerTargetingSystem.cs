using System.Linq;
using Content.Shared._ES.Objectives;
using Content.Shared._ES.SecretIdentity.Stalker.Components;
using Content.Shared._ES.Stagehand;
using Content.Shared.Localizations;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Utility;

namespace Content.Shared._ES.SecretIdentity.Stalker;

public sealed partial class ESStalkerTargetingSystem : EntitySystem
{
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private ESSharedObjectiveSystem _objective = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ESSharedSecretIdentitySystem _secretIdentity = default!;
    [Dependency] private ESSharedStagehandNotificationsSystem _stagehandNotifications = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESSelectStalkerTargetActionEvent>(OnSelectStalkerTarget);
        SubscribeLocalEvent<ESStalkerTargetingComponent, ESGetCharacterInfoBlurbEvent>(OnGetCharacterInfoBlurb);
    }

    private void OnGetCharacterInfoBlurb(Entity<ESStalkerTargetingComponent> ent, ref ESGetCharacterInfoBlurbEvent args)
    {
        var names = ContentLocalizationManager.FormatList(ent.Comp.TargetNames.Select(t => $"[color=yellow]{t}[/color]").ToList());
        args.Info.Add(FormattedMessage.FromMarkupPermissive(Loc.GetString("es-stalker-character-info-blurb", ("name", names))));
    }

    private void OnSelectStalkerTarget(ESSelectStalkerTargetActionEvent args)
    {
        if (!_mind.TryGetMind(args.Performer, out var mind) ||
            !_mind.TryGetMind(args.Target, out var targetMind))
            return;

        if (mind == targetMind)
            return;

        if (!_mobState.IsAlive(args.Target))
            return;

        var comp = EnsureComp<ESStalkerTargetingComponent>(mind.Value);
        if (comp.Targets.Contains(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("es-stalker-popup-already-stalking"), args.Performer, args.Performer);
            return;
        }

        var name = targetMind.Value.Comp.CharacterName ?? string.Empty;

        _popup.PopupEntity(Loc.GetString("es-stalker-popup-start-stalking", ("target", name)), args.Performer, args.Performer);

        var msg = Loc.GetString("es-stalker-stagehand-notif",
            ("player", _stagehandNotifications.WrapEntityName(args.Performer)),
            ("target", _stagehandNotifications.WrapEntityName(args.Target)));
        _stagehandNotifications.SendStagehandNotification(msg, ESStagehandNotificationSeverity.Low);

        comp.Targets.Add(args.Target);
        comp.TargetNames.Add(name);
        Dirty(mind.Value, comp);
        _secretIdentity.RefreshCharacterInfoBlurb(mind.Value.AsNullable());
        UpdateStalkerObjectives(mind.Value);

        EnsureComp<ESStalkerTargetComponent>(args.Target).OwningMind = mind.Value;

        args.Handled = true;
    }

    public void UpdateStalkerObjectives(EntityUid holder)
    {
        if (!TryComp<ESStalkerTargetingComponent>(holder, out var stalkerComp))
            return;

        foreach (var objective in _objective.GetObjectives<ESStalkerKillObjectiveComponent>(holder))
        {
            var counter = 0;
            var max = stalkerComp.Targets.Count;

            foreach (var target in stalkerComp.Targets)
            {
                if (TerminatingOrDeleted(target) ||
                    !Exists(target) ||
                    _mobState.IsDead(target))
                {
                    ++counter;
                }
            }

            if (objective.Comp.Invert)
                counter = max - counter;

            _objective.SetObjectiveCounter(objective.Owner, counter);
        }
    }
}
