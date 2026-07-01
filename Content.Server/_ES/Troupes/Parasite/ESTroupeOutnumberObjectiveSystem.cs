using Content.Server._ES.SecretIdentity;
using Content.Server._ES.Troupes.Parasite.Components;
using Content.Server.Mind;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared._ES.Objectives;
using Content.Shared._ES.Objectives.Components;
using Content.Shared.Mind;

namespace Content.Server._ES.Troupes.Parasite;

public sealed partial class ESTroupeOutnumberObjectiveSystem : ESBaseObjectiveSystem<ESTroupeOutnumberObjectiveComponent>
{
    [Dependency] private ESSecretIdentitySystem _secretIdentity = default!;
    [Dependency] private MindSystem _mind = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESSecretIdentityChangedEvent>(OnSecretIdentityChanged);
        SubscribeLocalEvent<ESPlayerKilledEvent>(OnPlayerKilled);
    }

    private void OnSecretIdentityChanged(ref ESSecretIdentityChangedEvent ev)
    {
        ObjectivesSys.RefreshObjectiveProgress<ESTroupeOutnumberObjectiveComponent>();
    }

    private void OnPlayerKilled(ref ESPlayerKilledEvent ev)
    {
        ObjectivesSys.RefreshObjectiveProgress<ESTroupeOutnumberObjectiveComponent>();
    }

    protected override void GetObjectiveProgress(Entity<ESTroupeOutnumberObjectiveComponent> ent, ref ESGetObjectiveProgressEvent args)
    {
        base.GetObjectiveProgress(ent, ref args);

        var troupeCount = 0;
        foreach (var mind in _secretIdentity.GetTroupeMembers(ent.Comp.Troupe))
        {
            if (!TryComp<MindComponent>(mind, out var mindComp))
                continue;

            if (!_mind.IsCharacterDeadIc(mindComp))
                ++troupeCount;
        }

        var nonTroupeCount = 0;
        foreach (var mind in _secretIdentity.GetNotTroupeMembers(ent.Comp.Troupe))
        {
            if (!TryComp<MindComponent>(mind, out var mindComp))
                continue;

            if (!_mind.IsCharacterDeadIc(mindComp))
                ++nonTroupeCount;
        }

        if (troupeCount == 0)
            return; // default progress = 0

        var percentage = nonTroupeCount == 0 ? 1f : (float) troupeCount / (troupeCount + nonTroupeCount);
        args.Progress = percentage == 0 ? 0 : percentage / ent.Comp.TargetPercentage;
    }
}
