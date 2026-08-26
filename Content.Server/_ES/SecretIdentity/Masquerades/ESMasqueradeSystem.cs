using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Content.Server.GameTicking.Rules;
using Content.Server.MassMedia.Systems;
using Content.Server.Mind;
using Content.Shared._Citadel.Utilities;
using Content.Shared._ES.Chat;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.SecretIdentity;
using Content.Shared._ES.SecretIdentity.Components;
using Content.Shared._ES.SecretIdentity.Masquerades;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Random.Helpers;
using Content.Shared.Station.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._ES.SecretIdentity.Masquerades;

/// <summary>
///     This handles masquerade management and how they influence game flow.
/// </summary>
public sealed partial class ESMasqueradeSystem : GameRuleSystem<ESMasqueradeRuleComponent>
{
    [Dependency] private IESSharedChatManager _chat = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ESEntityTimerSystem _timer = default!;
    [Dependency] private ESSecretIdentitySystem _secretIdentity = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private NewsSystem _news = default!;

    // Icky global state.
    private ProtoId<ESMasqueradePrototype>? _forcedMasquerade;

    public override Type[] RoundEndTextBefore => [typeof(ESSecretIdentitySystem)];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AssignLatejoinerToOrganizationEvent>(OnAssignLatejoiner);
        SubscribeLocalEvent<AssignPlayersToOrganizationEvent>(OnAssignPlayers);
    }

    private void OnAssignPlayers(ref AssignPlayersToOrganizationEvent ev)
    {
        var rule = EntityQuery<ESMasqueradeRuleComponent>().SingleOrDefault();

        if (rule?.Masquerade is null)
            return;

        var set = rule.Masquerade.Masquerade;

        if (!set.TryGetSecretIdentities(ev.Players.Count, rule.Rng, _proto, out var secretIdentities))
        {
            Log.Error($"Failed to assign secret identities for masquerade {rule.Masquerade!.ID}!");
            return;
        }

        if (rule.Masquerade.ImpersonateMasquerade is { } impersonate)
        {
            var proto = _proto.Index(impersonate);

            if (!proto.Masquerade.TryGetSecretIdentities(ev.Players.Count, rule.Rng, _proto, out var impersonationSecretIdentities))
            {
                Log.Error($"Failed to assign impersonation identities for masquerade {rule.Masquerade!.ID}!");
                return;
            }

            rule.AssignedSecretIdentities = impersonationSecretIdentities;
        }
        else
        {
            rule.AssignedSecretIdentities = secretIdentities.ShallowClone();
        }

        DebugTools.AssertEqual(secretIdentities.Count, ev.Players.Count, "Player count mismatched identity count, shit broke.");

        ev.Handled = true;

        // Add all of our game rules ahead of time so that they don't get started inside ApplySecretIdentity
        // This is because they may have logic that is dependent on having members assigned when they start.
        var organizationRules = new List<EntityUid>();
        foreach (var organizationId in GetOrganizationsFromMasquerade(rule.Masquerade, ev.Players.Count, rule.Seed.IntoRandomizer()))
        {
            var organization = _proto.Index(organizationId);
            organizationRules.Add(GameTicker.AddGameRule(organization.GameRule));
        }

        // Ensure no funny business with the player list, as the order masquerades output secret identities isn't random.
        rule.Rng.Shuffle(ev.Players);

        var players = ev.Players;

        var secretIdentitiesEnum = secretIdentities
            .OrderBy(m => _proto.Index(m).AssignmentOrder)
            .ThenByDescending(SecretIdentityOrder);

        foreach (var secretIdentityId in secretIdentitiesEnum)
        {
            var secretIdentity = _proto.Index(secretIdentityId);
            for (var i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (!TryGetMindOrLog(player, out var mind))
                    continue;

                if (!_secretIdentity.IsPlayerValid(secretIdentity, player))
                    continue;

                _secretIdentity.ApplySecretIdentity(mind.Value, secretIdentityId, applyModifiers: true);

                players.RemoveAt(i);
                goto exit; // escape to next identity.
            }

            // Ah hell, no dice, just take someone.

            for (var i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (!TryGetMindOrLog(player, out var mind))
                    continue;

                _secretIdentity.ApplySecretIdentity(mind.Value, secretIdentityId, applyModifiers: true);

                players.RemoveAt(i);
                goto exit; // escape to next identity.
            }

            // Fuuuck okay fine don't assign.

            Log.Error($"Was unable to assign {secretIdentityId} to any player.");

            exit: ;
        }

        // Now that all of our roles have been assigned, we can start the rules
        // Which will create objectives and run other logic as necessary.
        foreach (var organizationRule in organizationRules)
        {
            GameTicker.StartGameRule(organizationRule);
        }
    }

    private int SecretIdentityOrder(ProtoId<ESSecretIdentityPrototype> secretIdentityId)
    {
        var secretIdentity = _proto.Index(secretIdentityId);

        return secretIdentity.ProhibitedJobs.Count; // The tighter the prohibition list, the more careful we are.
    }

    private void OnAssignLatejoiner(ref AssignLatejoinerToOrganizationEvent ev)
    {
        var rule = EntityQuery<ESMasqueradeRuleComponent>().SingleOrDefault();

        if (rule?.Masquerade is null)
            return;

        var secretIdentity = rule.Masquerade.Masquerade.DefaultSecretIdentity.PickSecretIdentities(rule.Rng, _proto).Single();

        if (!TryGetMindOrLog(ev.Victim, out var mind))
            return;

        if (!TryGetOrganizationForSecretIdentityOrLog(secretIdentity, rule, out var organization))
            return;

        _secretIdentity.ApplySecretIdentity(mind.Value, secretIdentity, organization.Value, applyModifiers: true);
    }

    private bool TryGetOrganizationForSecretIdentityOrLog(ProtoId<ESSecretIdentityPrototype> secretIdentity,
        ESMasqueradeRuleComponent rule,
        [NotNullWhen(true)] out Entity<ESOrganizationRuleComponent>? organization)
    {
        if (!_secretIdentity.TryGetOrganizationEntityForSecretIdentity(secretIdentity, out organization))
        {
            Log.Error($"Failed to find a running organization for {secretIdentity}, is the masquerade {rule.Masquerade!.ID} missing a organization rule?");
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
        component.Masquerade = SelectMasquerade(GameTicker.ReadyPlayerCount());

        if (component.Masquerade is not {} masquerade)
            return;

        _chat.SendAdminMessage($"Upcoming masquerade is {masquerade.ID}.");

        foreach (var rule in masquerade.GameRules)
        {
            GameTicker.StartGameRule(rule);
        }

        // If we do news, run the news.
        if (masquerade.StartupNewsArticleTime is { } time)
        {
            _ = _timer.SpawnMethodTimer(time,
                () =>
                {
                    // Find The Station. Only one.
                    // and other places I wish the game had a Single<>() helper for "I really want to assume singleton".
                    var query = EntityQueryEnumerator<StationDataComponent>();

                    if (!query.MoveNext(out var ent, out _))
                        return;

                    if (component.Deleted)
                        return;

                    if (component.AssignedSecretIdentities == null)
                        return;

                    var report = new StringBuilder();

                    foreach (var secretIdentities in component.AssignedSecretIdentities.GroupBy(m => _proto.Index(m).Organization))
                    {
                        var organization = _proto.Index(secretIdentities.Key);

                        // If we need to obscure the secretIdentity name, do it here then don't list individual secretIdentity names
                        if (organization.DisguisedSecretIdentityName is { } disguisedSecretIdentityName)
                        {
                            report.AppendLine(Loc.GetString(masquerade.StartupNewsArticleSecretIdentityEntry,
                                ("count", secretIdentities.Count()),
                                ("secretIdentity", Loc.GetString(disguisedSecretIdentityName))));
                            continue;
                        }

                        foreach (var (secretIdentityId, count) in secretIdentities.CountBy(x => x))
                        {
                            report.AppendLine(Loc.GetString(masquerade.StartupNewsArticleSecretIdentityEntry,
                                ("count", count),
                                ("secretIdentity", Loc.GetString(_proto.Index(secretIdentityId).Name))));
                        }
                    }

                    _news.TryAddNews(ent,
                        Loc.GetString(masquerade.StartupNewsArticleTitle),
                        Loc.GetString(masquerade.StartupNewsArticleContents, ("secretIdentityEntries", report)),
                        out _,
                        enforceLimits: false);
                });
        }
    }

    private ESMasqueradePrototype? SelectMasquerade(int players)
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
                .ToDictionary(x => x, x => x.Weight!.Value);

            if (weighted.Count == 0)
                return null;

            return _random.Pick(weighted);
        }
    }

    /// <summary>
    /// For a given masquerade at a specified playercount and random seed, returns the organizations that will be present.
    /// </summary>
    public HashSet<ProtoId<ESOrganizationPrototype>> GetOrganizationsFromMasquerade(ESMasqueradePrototype masquerade, int playerCount, IRobustRandom random)
    {
        // Try and get the unique secretIdentities we'll have at this pop level for this seed
        if (!masquerade.Masquerade.TryGetSecretIdentities(playerCount, random, _proto,  out var secretIdentities))
            return [];

        foreach (var secretIdentity in masquerade.Masquerade.DefaultSecretIdentity.PickSecretIdentities(random, _proto))
        {
            secretIdentities.Add(secretIdentity);
        }

        var organizations = new HashSet<ProtoId<ESOrganizationPrototype>>();
        foreach (var secretIdentity in secretIdentities)
        {
            organizations.Add(_proto.Index(secretIdentity).Organization);
        }

        return organizations;
    }

    public bool TryGetMasqueradeData([NotNullWhen(true)] out MasqueradeRoleSet? set)
    {
        set = null;
        var rule = EntityQuery<ESMasqueradeRuleComponent>().SingleOrDefault();

        if (rule?.Masquerade is null)
            return false;

        set = rule.Masquerade.Masquerade;

        return true;
    }
}
