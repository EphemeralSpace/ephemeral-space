using System.Diagnostics.CodeAnalysis;
using Content.Shared._ES.Objectives.Components;
using Content.Shared._ES.Objectives.Target.Components;
using Content.Shared.Actions;
using Content.Shared.Localizations;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Network;

namespace Content.Shared._ES.Objectives.Target;

public sealed class ESSenseTargetDistanceSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ESSharedObjectiveSystem _objectives = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ESTargetObjectiveSystem _targetObjective = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESSenseTargetDistanceActionEvent>(OnSenseTargetDistance);
    }

    private void OnSenseTargetDistance(ESSenseTargetDistanceActionEvent args)
    {
        if (!_mind.TryGetMind(args.Performer, out var mind, out _))
            return;

        // Mark it as handled on the client for responsiveness, despite not doing anything.
        args.Handled = true;

        if (_netManager.IsClient)
            return;

        var popup = "es-avenger-sense-gone";
        var popupType = PopupType.Medium;
        var direction = "es-avenger-direction-unknown";

        var userXform = Transform(args.Performer);
        var gridRotation = userXform.GridUid == null ? Angle.Zero : Transform(userXform.GridUid.Value).LocalRotation;

        if (TryGetTarget(mind, args.ObjectiveWhitelist, out var target)
            && Transform(target.Value) is { } targetXform
            && targetXform.MapID == userXform.MapID)
        {
            var mapCoordinates = _transform.ToMapCoordinates(userXform.Coordinates);
            var otherMapCoordinates = _transform.ToMapCoordinates(targetXform.Coordinates);

            var delta = otherMapCoordinates.Position - mapCoordinates.Position;
            var distance = delta.Length();
            var dir = delta.ToWorldAngle();
            var dirString = ContentLocalizationManager.FormatDirection((dir - gridRotation).GetDir()).ToLowerInvariant();
            direction = Loc.GetString("es-avenger-direction", ("dir", dirString));

            (popup, popupType) = distance switch
            {
                <= 3.5f => ("es-avenger-sense-adjacent", PopupType.LargeCaution),
                <= 15f => ("es-avenger-sense-close", PopupType.Large),
                <= 30f => ("es-avenger-sense-moderate", PopupType.Large),
                _ => ("es-avenger-sense-far", PopupType.Medium),
            };
        }

        _popup.PopupEntity(Loc.GetString(popup, ("direction", direction)), args.Performer, args.Performer, popupType);
    }

    private bool TryGetTarget(Entity<ESObjectiveHolderComponent?> ent, EntityWhitelist whitelist, [NotNullWhen(true)] out EntityUid? target)
    {
        target = null;
        if (!Resolve(ent, ref ent.Comp))
            return false;

        foreach (var objective in _objectives.GetObjectives<ESTargetObjectiveComponent>(ent))
        {
            if (_entityWhitelist.IsWhitelistFail(whitelist, objective))
                continue;
            target = _targetObjective.GetTargetOrNull(objective.AsNullable());
            return target != null;
        }

        return false;
    }
}

public sealed partial class ESSenseTargetDistanceActionEvent : InstantActionEvent
{
    [DataField]
    public EntityWhitelist ObjectiveWhitelist = new();
}
