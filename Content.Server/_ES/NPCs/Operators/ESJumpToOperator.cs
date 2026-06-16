using System.Numerics;
using Content.Server.NPC.HTN.PrimitiveTasks.Operators.Interactions;
using Content.Shared.CombatMode;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Map;
using Robust.Shared.Physics;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Combat.Melee;

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
    public float ThrowSpeedKey = 5f;

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
