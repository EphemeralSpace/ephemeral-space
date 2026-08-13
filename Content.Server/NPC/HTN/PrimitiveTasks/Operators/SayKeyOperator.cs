using Content.Server._ES.Chat;
using Content.Shared._ES.Chat;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class SayKeyOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    private ESChatSystem _chat = default!;

    [DataField(required: true)]
    public string Key = string.Empty;

    /// <summary>
    /// Whether to hide message from chat window and logs.
    /// </summary>
    [DataField]
    public bool Hidden;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);

        _chat = sysManager.GetEntitySystem<ESChatSystem>();
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        if (!blackboard.TryGetValue<object>(Key, out var value, _entManager))
            return HTNOperatorStatus.Failed;

        var @string = value.ToString();
        if (@string is not { })
            return HTNOperatorStatus.Failed;

        var speaker = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        _chat.TrySendMessage(@string, ESSharedChatSystem.LocalChannel, speaker, hideChat: Hidden, logOverride: Hidden);

        return base.Update(blackboard, frameTime);
    }
}
