using System.Linq;
using Content.Server._ES.Stagehand;
using Content.Server.Mind;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared._ES.SecretIdentity;
using Content.Shared.Mind;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._ES.SecretIdentity.Superfan;

/// <seealso cref="ESSuperfanComponent"/>
public sealed partial class ESSuperfanSystem : EntitySystem
{
    [Dependency] private ESSecretIdentitySystem _secretIdentity = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private ESStagehandNotificationsSystem _stagehandNotifications = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESPlayerKilledEvent>(OnKillReported);
        SubscribeLocalEvent<ESSecretIdentityChangedEvent>(OnSecretIdentityChanged);
    }

    private void OnKillReported(ref ESPlayerKilledEvent ev)
    {
        if (!_secretIdentity.TryGetOrganization(ev.Killed, out var organization))
            return;

        TryConvert(organization.Value);
    }

    private void OnSecretIdentityChanged(ref ESSecretIdentityChangedEvent ev)
    {
        // only check when identity is being removed
        if (ev.NewSecretIdentity != null || ev.OldSecretIdentity == null)
            return;

        TryConvert(ev.OldSecretIdentity.Organization);
    }

    private void TryConvert(ProtoId<ESOrganizationPrototype> organization)
    {
        var fanQuery = EntityQueryEnumerator<ESSuperfanComponent, MindComponent>();
        while (fanQuery.MoveNext(out var ent, out var comp, out var mind))
        {
            if (organization != comp.TargetOrganization)
                continue;

            var total = 0;
            var dead = 0;
            foreach (var member in _secretIdentity.GetOrganizationMembers(comp.TargetOrganization))
            {
                total += 1;

                if (_mind.IsCharacterDeadIc(Comp<MindComponent>(member)))
                    dead += 1;
            }

            // Chance to be converted is proportional to the number of dead organization members.
            var prob = total != 0
                ? (float)dead / total
                : 1;

            if (!_random.Prob(prob))
                continue;

            if (_mind.IsCharacterDeadIc(mind))
                continue; // Don't assign the dead to tot identities.

            if (mind.OwnedEntity.HasValue)
            {
                var msg = Loc.GetString("es-sleeper-agent-activate-stagehand-notif",
                    ("name", _stagehandNotifications.WrapEntityName(mind.OwnedEntity.Value)));
                _stagehandNotifications.SendStagehandNotification(msg);
            }

            _secretIdentity.ChangeSecretIdentity((ent, mind), comp.TargetSecretIdentity.PickSecretIdentities(_random, _proto).Single());
        }
    }
}
