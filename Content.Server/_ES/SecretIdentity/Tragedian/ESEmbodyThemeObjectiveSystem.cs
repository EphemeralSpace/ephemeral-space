using Content.Server._ES.SecretIdentity.Objectives.Relays.Components;
using Content.Server._ES.SecretIdentity.Tragedian.Components;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared._ES.Objectives;
using Content.Shared._ES.Objectives.Components;
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
        SubscribeLocalEvent<ESEmbodyThemeVoteComponent, ESVoteCompletedEvent>(OnVoteCompleted);
    }

    private void OnPlayerKilled(Entity<ESEmbodyThemeObjectiveComponent> ent, ref ESPlayerKilledEvent args)
    {
        if (!MindSys.TryGetMind(args.Killed, out var mind))
            return;

        var voteTitle = Loc.GetString(ent.Comp.VoteTitle,
            ("name", mind.Value.Comp.CharacterName ?? string.Empty),
            ("theme", ent.Comp.Theme));

        // This is really ugly and i'm kicking myself for not making a better API
        var vote = Spawn(ent.Comp.VoteEntity, doMapInit: false);
        var metaData = MetaData(vote);
        _metaData.SetEntityName(vote, voteTitle, metaData);
        EntityManager.RunMapInit(vote, metaData);

        var comp = EnsureComp<ESEmbodyThemeVoteComponent>(vote);
        comp.Objective = ent;
    }

    private void OnVoteCompleted(Entity<ESEmbodyThemeVoteComponent> ent, ref ESVoteCompletedEvent args)
    {
        if (args.Result is not ESBooleanVoteOption option)
            return;

        if (option.Value)
            ObjectivesSys.AdjustObjectiveCounter(ent.Comp.Objective);
    }

    protected override void InitializeObjective(Entity<ESEmbodyThemeObjectiveComponent> ent, ref ESInitializeObjectiveEvent args)
    {
        var dataset = _prototype.Index(ent.Comp.ThemeDataset);
        ent.Comp.Theme = _random.Pick(dataset);

        _metaData.SetEntityName(ent, Loc.GetString(ent.Comp.Title, ("theme", ent.Comp.Theme)));
    }
}
