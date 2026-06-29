using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.Throwing;
using Robust.Shared.Map;

namespace Content.Server._ES.NPCs.Operators;

public sealed partial class ESJumpToOperator : HTNOperator
{
    private ThrowingSystem _throwingSystem = default!;

    /// <summary>
    /// Key that contains the target entity.
    /// </summary>
    [DataField(required: true)]
    public string TargetKey = default!;

    /// <summary>
    /// Key that contains throw speed
    /// </summary>
    [DataField]
    public float ThrowSpeedKey = 10f;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _throwingSystem = sysManager.GetEntitySystem<ThrowingSystem>();
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var targetCoordinates = blackboard.GetValue<EntityCoordinates>(TargetKey);

        _throwingSystem.TryThrow(owner, targetCoordinates, ThrowSpeedKey);

        return HTNOperatorStatus.Finished;
    }
}
