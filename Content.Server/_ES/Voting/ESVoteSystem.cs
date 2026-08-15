using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Shared._ES.Chat;
using Content.Shared._ES.Voting;
using Content.Shared._ES.Voting.Components;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Toolshed;

namespace Content.Server._ES.Voting;

/// <inheritdoc/>
public sealed partial class ESVoteSystem : ESSharedVoteSystem
{
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IESSharedChatManager _chat = default!;

    private static readonly SoundSpecifier VoteSound = new SoundPathSpecifier("/Audio/Effects/voteding.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRuleComponent, ESSynchronizedVotesPostCompletedEvent>(OnPostCompleted);
        SubscribeLocalEvent<ESBeforeRoundEndEvent>(OnBeforeRoundEnd);
    }

    private void OnPostCompleted(Entity<GameRuleComponent> ent, ref ESSynchronizedVotesPostCompletedEvent args)
    {
        // We manually start this rule now that the votes have concluded.
        // Is this kinda hacky? yes. I don't think it's that bad though
        Comp<GameRuleComponent>(ent).Added = true;
        var ev = new GameRuleAddedEvent(ent, Prototype(ent)!.ID);
        RaiseLocalEvent(ent, ref ev, true);
    }

    private void OnBeforeRoundEnd(ref ESBeforeRoundEndEvent ev)
    {
        var votes = new List<Entity<ESVoteComponent>>();
        foreach (var ent in EntityQueryEnumerator<ESEndBeforeRoundEndVoteComponent, ESVoteComponent>())
        {
            votes.Add((ent, ent.Comp2));
        }

        foreach (var vote in votes)
        {
            EndVote(vote);
        }
    }

    protected override void SendVoteStartAnnouncement(Entity<ESVoteComponent> ent)
    {
        var voters = new List<ICommonSession>();
        var query = EntityQueryEnumerator<ESVoterComponent, ActorComponent>();
        while (query.MoveNext(out _, out _, out var actor))
        {
            voters.Add(actor.PlayerSession);
        }

        var msg = Loc.GetString("es-voter-chat-announce-result",
            ("query", Loc.GetString("es-voter-chat-announce-vote-start")),
            ("result", Name(ent)));
        var wrappedMsg = Loc.GetString("es-voter-chat-announce-wrap-message", ("message", msg));
        _chat.SendChatMessage(wrappedMsg,
            voters,
            IESSharedChatManager.ServerChannel,
            null,
            sound: VoteSound,
            color: Color.Plum);
        _adminLog.Add(LogType.Vote, LogImpact.Medium, $"Started vote for {ToPrettyString(ent)}.");
    }

    protected override void SendVoteResultAnnouncement(Entity<ESVoteComponent> ent, ESVoteOption result)
    {
        var voters = new List<ICommonSession>();
        var query = EntityQueryEnumerator<ESVoterComponent, ActorComponent>();
        while (query.MoveNext(out _, out _, out var actor))
        {
            voters.Add(actor.PlayerSession);
        }

        var msg = Loc.GetString("es-voter-chat-announce-result",
            ("query", Loc.GetString(ent.Comp.QueryString)),
            ("result", result.DisplayString));
        var wrappedMsg = Loc.GetString("es-voter-chat-announce-wrap-message", ("message", msg));
        _chat.SendChatMessage(wrappedMsg,
            voters,
            IESSharedChatManager.ServerChannel,
            null,
            sound: VoteSound,
            color: Color.Plum);
        _adminLog.Add(LogType.Vote, LogImpact.Medium, $"Finished vote for {ToPrettyString(ent)}. Vote conclusion: \"{msg}\"");
    }
}

[ToolshedCommand, AdminCommand(AdminFlags.Fun)]
public sealed class ESVoteCommand : ToolshedCommand
{
    [CommandImplementation("ls")]
    public IEnumerable<Entity<ESVoteComponent>> List()
    {
        var query = EntityManager.EntityQueryEnumerator<ESVoteComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            yield return (uid, comp);
        }
    }

    [CommandImplementation("options")]
    public IEnumerable<string> Options([PipedArgument] Entity<ESVoteComponent> vote)
    {
        foreach (var option in vote.Comp.VoteOptions)
        {
            yield return option.DisplayString;
        }
    }

    [CommandImplementation("tally")]
    public IEnumerable<string> Tally([PipedArgument] Entity<ESVoteComponent> vote)
    {
        foreach (var (option, votes) in vote.Comp.Votes)
        {
            yield return $"{option.DisplayString}: {votes.Count}";
        }
    }

    [CommandImplementation("end")]
    public void End([PipedArgument] Entity<ESVoteComponent> vote)
    {
        var sys = Sys<ESVoteSystem>();
        sys.EndVote(vote);
    }
}
