using Content.Server.Electrocution;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.Power.Components;

namespace Content.Server._ES.NPCs.Operators;

public sealed partial class ESSnipCableOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    private ElectrocutionSystem _electrocution;

    /// <summary>
    /// Key that contains the target entity.
    /// </summary>
    [DataField]
    public string TargetKey = "Target";

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _electrocution = sysManager.GetEntitySystem<ElectrocutionSystem>();
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entityManager))
            return HTNOperatorStatus.Failed;

        if (!_entityManager.TryGetComponent<CableComponent>(target, out var cableComponent))
            return HTNOperatorStatus.Failed;

        _electrocution.TryDoElectrifiedAct(target, owner);
        _entityManager.SpawnNextToOrDrop(cableComponent.CableDroppedOnCutPrototype, target);
        _entityManager.QueueDeleteEntity(target);

        return HTNOperatorStatus.Finished;
    }
}
