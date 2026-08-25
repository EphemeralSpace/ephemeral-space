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
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Content.Shared.Random.Helpers;
using Content.Shared.Station.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._ES.SecretIdentity.Masquerades;

// MISC TODO: Remove news integration and unused report type stuff

/// <summary>
///     This handles masquerade management and how they influence game flow.
/// </summary>
public sealed partial class ESMasqueradeSystem : GameRuleSystem<ESMasqueradeRuleComponent>
{
    [Dependency] private IESSharedChatManager _chat = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
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

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    public Dictionary<NetUserId, ProtoId<ESSecretIdentityPrototype>> AssignMasquerade(
        Dictionary<NetUserId, HumanoidCharacterProfile> profiles)
    {
        if (!TrySingle<ESMasqueradeRuleComponent>(out var rule) ||
            rule.Value.Comp.Masquerade is not { } masquerade)
        {
            return [];
        }

        var random = rule.Value.Comp.Rng;

        var playerCount = profiles.Count;
        var roleSet = masquerade.Masquerade;

        if (!roleSet.TryGetSecretIdentities(playerCount, random, _proto, out var secretIdentities))
        {
            Log.Error($"Failed to assign secret identities for masquerade {masquerade.ID}!");
            return [];
        }

        DebugTools.AssertEqual(secretIdentities.Count, playerCount, "Player count mismatched identity count, shit broke.");

        var players = new List<NetUserId>(profiles.Keys);
        random.Shuffle(players);

        var assignments = new Dictionary<NetUserId, ProtoId<ESSecretIdentityPrototype>>();
        foreach (var secretIdentityId in secretIdentities)
        {
            var secretIdentity = _proto.Index(secretIdentityId);

            var validPlayers = players
                .Where(u => _secretIdentity.CanAssignSecretIdentity(u, secretIdentity))
                .ToList();

            // If there's no valid players, just pick someone random out of the pool.
            // SId restrictions are weakly binding. They have priority over basically
            // everything in terms of assignment.
            var player = validPlayers.Count == 0
                ? random.Pick(players)
                : random.Pick(validPlayers);

            assignments.Add(player, secretIdentity);
            players.Remove(player);
        }

        return assignments;
    }

    public void InitializeMasquerade(Dictionary<NetUserId, ProtoId<ESSecretIdentityPrototype>> secretIdentities)
    {
        if (!TrySingle<ESMasqueradeRuleComponent>(out var rule))
            return;

        var random = rule.Value.Comp.Rng;

        var players = new List<NetUserId>(secretIdentities.Keys);
        random.Shuffle(players);

        var organizationRules = new List<EntityUid>();
        var organizationIds = secretIdentities.Values
            .Select(_proto.Index)
            .Select(p => p.Organization)
            .ToHashSet();

        // Add all of our game rules ahead of time so that they don't get started inside ApplySecretIdentity
        // This is because they may have logic that is dependent on having members assigned when they start.
        foreach (var organizationId in organizationIds)
        {
            var organization = _proto.Index(organizationId);
            organizationRules.Add(GameTicker.AddGameRule(organization.GameRule));
        }

        var orderedSecretIdentities = secretIdentities
            .OrderBy(m => _proto.Index(m.Value).AssignmentOrder);

        foreach (var (user, secretIdentityId) in orderedSecretIdentities)
        {
            var session = _player.GetSessionById(user);
            if (!TryGetMindOrLog(session, out var mind))
            {
                Log.Warning($"Failed to get mind for session {session}!");
                return;
            }

            _secretIdentity.ApplySecretIdentity(mind.Value, secretIdentityId);
        }

        // Now that all of our roles have been assigned, we can start the rules
        // Which will create objectives and run other logic as necessary.
        foreach (var organizationRule in organizationRules)
        {
            GameTicker.StartGameRule(organizationRule);
        }
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (!ev.LateJoin)
            return;

        ApplyLateJoinSecretIdentity(ev.Player);
    }

    public void ApplyLateJoinSecretIdentity(ICommonSession session)
    {
        if (!TrySingle<ESMasqueradeRuleComponent>(out var rule) ||
            rule.Value.Comp.Masquerade is not { } masquerade)
        {
            return;
        }

        var random = rule.Value.Comp.Rng;

        var secretIdentity = masquerade.Masquerade.DefaultSecretIdentity.PickSecretIdentities(random, _proto).Single();

        if (!TryGetMindOrLog(session, out var mind))
            return;

        if (!TryGetOrganizationForSecretIdentityOrLog(secretIdentity, rule, out var organization))
            return;

        _secretIdentity.ApplySecretIdentity(mind.Value, secretIdentity, organization.Value);
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
