using Content.Server._ES.SecretIdentity;
using Content.Server._ES.Organizations.Parasite.Components;
using Content.Server.Mind;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared._ES.Objectives;
using Content.Shared._ES.Objectives.Components;
using Content.Shared.Mind;

namespace Content.Server._ES.Organizations.Parasite;

public sealed partial class ESOrganizationOutnumberObjectiveSystem : ESBaseObjectiveSystem<ESOrganizationOutnumberObjectiveComponent>
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
        ObjectivesSys.RefreshObjectiveProgress<ESOrganizationOutnumberObjectiveComponent>();
    }

    private void OnPlayerKilled(ref ESPlayerKilledEvent ev)
    {
        ObjectivesSys.RefreshObjectiveProgress<ESOrganizationOutnumberObjectiveComponent>();
    }

    protected override void GetObjectiveProgress(Entity<ESOrganizationOutnumberObjectiveComponent> ent, ref ESGetObjectiveProgressEvent args)
    {
        base.GetObjectiveProgress(ent, ref args);

        var organizationCount = 0;
        foreach (var mind in _secretIdentity.GetOrganizationMembers(ent.Comp.Organization))
        {
            if (!TryComp<MindComponent>(mind, out var mindComp))
                continue;

            if (!_mind.IsCharacterDeadIc(mindComp))
                ++organizationCount;
        }

        var nonOrganizationCount = 0;
        foreach (var mind in _secretIdentity.GetNotOrganizationMembers(ent.Comp.Organization))
        {
            if (!TryComp<MindComponent>(mind, out var mindComp))
                continue;

            if (!_mind.IsCharacterDeadIc(mindComp))
                ++nonOrganizationCount;
        }

        if (organizationCount == 0)
            return; // default progress = 0

        var percentage = nonOrganizationCount == 0 ? 1f : (float) organizationCount / (organizationCount + nonOrganizationCount);
        args.Progress = percentage == 0 ? 0 : percentage / ent.Comp.TargetPercentage;
    }
}
