using System.Linq;
using Content.Shared._ES.Objectives;
using Content.Shared.Actions;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Masks.Aura;

public sealed class ESSenseAuraSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly ESSharedMaskSystem _mask = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ESSharedObjectiveSystem _objective = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESSenseAuraEvent>(OnSenseAuraEvent);
    }

    private void OnSenseAuraEvent(ESSenseAuraEvent args)
    {
        // This can't be predicted but there's no harm in having it in shared.
        if (!_mind.TryGetMind(args.Target, out var mind, out var mindComp) ||
            !_mask.TryGetMask((mind, mindComp), out var mask))
            return;

        var auras = _prototype.EnumeratePrototypes<ESAuraPrototype>().ToList();
        var candidateAuras = auras.Where(a => a.ObjectiveWhitelist is null).ToHashSet();

        foreach (var objective in _objective.GetObjectives(mind))
        {
            foreach (var aura in auras)
            {
                if (aura.MaskOverrides.Contains(mask.Value) ||
                    _entityWhitelist.IsWhitelistPass(aura.ObjectiveWhitelist, objective))
                    candidateAuras.Add(aura);
            }
        }

        if (candidateAuras.MaxBy(a => a.Priority) is not { } primaryAura)
            return;

        var msg = Loc.GetString("es-aura-sense-popup", ("aura", Loc.GetString(primaryAura.Description)));
        _popup.PopupEntity(msg, args.Target, args.Performer, primaryAura.PopupType);
        args.Handled = true;
    }
}

public sealed partial class ESSenseAuraEvent : EntityTargetActionEvent;
