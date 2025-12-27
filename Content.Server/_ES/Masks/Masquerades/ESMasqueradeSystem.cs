using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._ES.Masks.Masquerades;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Shared._Citadel.Utilities;
using Content.Shared._ES.Masks;
using Content.Shared._ES.Masks.Components;
using Content.Shared._ES.Masks.Masquerades;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Random.Helpers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._ES.Masks.Masquerades;

/// <summary>
///     This handles masquerade management and how they influence game flow.
/// </summary>
public sealed class ESMasqueradeSystem : GameRuleSystem<ESMasqueradeRuleComponent>
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly ESMaskSystem _mask = default!;
    [Dependency] private readonly MindSystem _mind = default!;

    // Icky global state.
    private ProtoId<ESMasqueradePrototype>? _forcedMasquerade = null;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AssignLatejoinerToTroupeEvent>(OnAssignLatejoiner);
        SubscribeLocalEvent<AssignPlayersToTroupeEvent>(OnAssignPlayers);
    }

    private void OnAssignPlayers(ref AssignPlayersToTroupeEvent ev)
    {
        var rule = EntityQuery<ESMasqueradeRuleComponent>().SingleOrDefault();

        if (rule is null)
            return;

        // TODO: This shouldn't be hardcoded here, TryGetMasks should be on MasqueradeKind.
        if (rule.Masquerade!.Masquerade is not MasqueradeRoleSet set)
            return;

        if (!set.TryGetMasks(ev.Players.Count, rule.Rng, _proto, out var masks))
        {
            Log.Error($"Failed to assign masks for masquerade {rule.Masquerade!.ID}!");
            return;
        }

        DebugTools.AssertEqual(masks.Count, ev.Players.Count, "Player count mismatched mask count, shit broke.");

        ev.Handled = true;

        // Ensure no funny business with the player list, as the order masquerades output masks isn't random.
        rule.Rng.Shuffle(ev.Players);

        var players = ev.Players;

        var masksEnum = masks.OrderByDescending(MaskOrder);

        foreach (var mask in masksEnum)
        {
            var troupe = _proto.Index(_proto.Index(mask).Troupe);
            for (var i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (!TryGetMindOrLog(player, out var mind))
                    continue;

                if (!_mask.IsPlayerValid(troupe, player))
                    continue;

                if (!TryGetTroupeForMaskOrLog(mask, rule, out var troupeEnt))
                    return;

                _mask.ApplyMask(mind.Value, mask, troupeEnt.Value);

                players.RemoveAt(i);
                goto exit; // escape to next mask.
            }

            // Ah hell, no dice, just take someone.

            for (var i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (!TryGetMindOrLog(player, out var mind))
                    continue;

                if (!TryGetTroupeForMaskOrLog(mask, rule, out var troupeEnt))
                    return;

                _mask.ApplyMask(mind.Value, mask, troupeEnt.Value);

                players.RemoveAt(i);
                goto exit; // escape to next mask.
            }
            // Fuuuck okay fine don't assign.

            Log.Error($"Was unable to assign {mask} to any player.");

            exit: ;
        }
    }

    private int MaskOrder(ProtoId<ESMaskPrototype> mask)
    {
        var troupe = _proto.Index(_proto.Index(mask).Troupe);

        return troupe.ProhibitedJobs.Count; // The tighter the prohibition list, the more careful we are.
    }

    private void OnAssignLatejoiner(ref AssignLatejoinerToTroupeEvent ev)
    {
        var rule = EntityQuery<ESMasqueradeRuleComponent>().SingleOrDefault();

        if (rule is null)
            return;

        var mask = rule.Masquerade!.Masquerade.DefaultMask.PickMasks(rule.Rng, _proto).Single();

        if (!TryGetMindOrLog(ev.Victim, out var mind))
            return;

        if (!TryGetTroupeForMaskOrLog(mask, rule, out var troupe))
            return;

        _mask.ApplyMask(mind.Value, mask, troupe.Value);
    }

    private bool TryGetTroupeForMaskOrLog(ProtoId<ESMaskPrototype> mask,
        ESMasqueradeRuleComponent rule,
        [NotNullWhen(true)] out Entity<ESTroupeRuleComponent>? troupe)
    {
        if (!_mask.TryGetTroupeEntityForMask(mask, out troupe))
        {
            Log.Error($"Failed to find a running troupe for {mask}, is the masquerade {rule.Masquerade!.ID} missing a troupe rule?");
            return false;
        }

        return true;
    }

    private bool TryGetMindOrLog(ICommonSession target, [NotNullWhen(true)] out Entity<MindComponent>? mind)
    {
        if (!_mind.TryGetMind(target, out var mindEnt, out var mindComp))
        {
            Log.Error($"Failed to get mind for session {target}");
            mind = null;
            return false;
        }

        mind = (mindEnt, mindComp);
        return true;
    }


    /// <summary>
    ///     Force the given masquerade, or clear it if null.
    /// </summary>
    /// <param name="proto"></param>
    public void ForceMasquerade(ProtoId<ESMasqueradePrototype>? proto)
    {
        _forcedMasquerade = proto;
    }

    protected override void Started(EntityUid uid, ESMasqueradeRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        // Random seed to roll with.
        component.Seed = new RngSeed(_random);
        component.Rng = component.Seed.IntoRandomizer();
        component.Masquerade = SelectMasquerade(_gameTicker.ReadyPlayerCount());

        _chat.SendAdminAlert($"Upcoming masquerade is {component.Masquerade.ID}.");

        // TODO: Masquerades should auto-discover the necessary troupe rules.
        foreach (var rule in component.Masquerade.GameRules)
        {
            _gameTicker.StartGameRule(rule);
        }
    }

    private ESMasqueradePrototype SelectMasquerade(int players)
    {
        if (_forcedMasquerade is { } forced)
        {
            return _proto.Index(forced);
        }
        else
        {
            var weighted = _proto.EnumeratePrototypes<ESMasqueradePrototype>()
                .Where(x => x.Weight is not null)
                .Where(x => players >= x.Masquerade.MinPlayers && (x.Masquerade.MaxPlayers >= players || x.Masquerade.MaxPlayers is null))
                .ToDictionary(x => x, x => (float)x.Weight!.Value);

            return _random.Pick(weighted);
        }
    }
}
