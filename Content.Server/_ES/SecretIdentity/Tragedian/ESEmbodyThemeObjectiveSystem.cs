using Content.Server._ES.SecretIdentity.Objectives.Relays.Components;
using Content.Server._ES.SecretIdentity.Tragedian.Components;
using Content.Shared._ES.Auditions.Components;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared._ES.Objectives;
using Content.Shared._ES.Objectives.Components;
using Content.Shared._ES.SecretIdentity;
using Content.Shared._ES.SecretIdentity.Components;
using Content.Shared._ES.Voting.Components;
using Content.Shared._ES.Voting.Results;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._ES.SecretIdentity.Tragedian;

public sealed partial class ESEmbodyThemeObjectiveSystem : ESBaseObjectiveSystem<ESEmbodyThemeObjectiveComponent>
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MetaDataSystem _metaData = default!;

    public override Type[] RelayComponents => [typeof(ESKilledRelayComponent)];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESEmbodyThemeObjectiveComponent, ESPlayerKilledEvent>(OnPlayerKilled);
        SubscribeLocalEvent<ESSecretIdentityChangedEvent>(OnSecretIdentityChanged);
        SubscribeLocalEvent<ESEmbodyThemeVoteComponent, ESVoteCompletedEvent>(OnVoteCompleted);
    }

    private void OnPlayerKilled(Entity<ESEmbodyThemeObjectiveComponent> ent, ref ESPlayerKilledEvent args)
    {
        RunVote(ent);
    }

    private void OnSecretIdentityChanged(ref ESSecretIdentityChangedEvent args)
    {
        foreach (var objective in ObjectivesSys.GetObjectives<ESEmbodyThemeObjectiveComponent>(args.Mind.Owner))
        {
            if (!TryComp<ESSecretIdentityObjectiveComponent>(objective, out var comp) ||
                comp.AssociatedIdentity == args.NewSecretIdentity?.ID)
                continue;

            RunVote(objective);
        }
    }

    private void OnVoteCompleted(Entity<ESEmbodyThemeVoteComponent> ent, ref ESVoteCompletedEvent args)
    {
        if (args.Result is not ESBooleanVoteOption option)
            return;

        if (option.Value)
            ObjectivesSys.AdjustObjectiveCounter(ent.Comp.Objective);
    }

    public void RunVote(Entity<ESEmbodyThemeObjectiveComponent> ent)
    {
        if (ent.Comp.VoteRan)
            return;

        if (!ObjectivesSys.TryFindObjectiveHolder(ent.Owner, out var holder) ||
            !TryComp<ESCharacterComponent>(holder, out var character))
            return;

        ent.Comp.VoteRan = true;

        var voteTitle = Loc.GetString(ent.Comp.VoteTitle,
            ("name", character.Name),
            ("theme", ent.Comp.Theme));

        // This is really ugly and i'm kicking myself for not making a better API
        var vote = Spawn(ent.Comp.VoteEntity, doMapInit: false);
        var metaData = MetaData(vote);
        _metaData.SetEntityName(vote, voteTitle, metaData);
        EntityManager.RunMapInit(vote, metaData);

        var comp = EnsureComp<ESEmbodyThemeVoteComponent>(vote);
        comp.Objective = ent;
    }

    protected override void InitializeObjective(Entity<ESEmbodyThemeObjectiveComponent> ent, ref ESInitializeObjectiveEvent args)
    {
        var dataset = _prototype.Index(ent.Comp.ThemeDataset);
        ent.Comp.Theme = _random.Pick(dataset);

        _metaData.SetEntityName(ent, Loc.GetString(ent.Comp.Title, ("theme", ent.Comp.Theme)));
    }
}
