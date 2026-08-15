using System.Linq;
using Content.Server._ES.SecretIdentity.Masquerades;
using Content.Server._ES.Stagehand;
using Content.Server.Mind;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared._ES.SecretIdentity;
using Content.Shared._ES.SecretIdentity.Components;
using Content.Shared.Mind;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._ES.SecretIdentity.Superfan;

/// <seealso cref="ESSuperfanComponent"/>
public sealed partial class ESSuperfanSystem : EntitySystem
{
    [Dependency] private ESSecretIdentitySystem _secretIdentity = default!;
    [Dependency] private ESMasqueradeSystem _masquerade = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private ESStagehandNotificationsSystem _stagehandNotifications = default!;

    private static readonly ProtoId<ESOrganizationPrototype> TraitorsOrganization = "Traitor";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESPlayerKilledEvent>(OnKillReported);
        SubscribeLocalEvent<ESSecretIdentityChangedEvent>(OnSecretIdentityChanged);
    }

    private void OnKillReported(ref ESPlayerKilledEvent ev)
    {
        // Only activate if our target organization died.
        if (_secretIdentity.GetOrganizationOrNull(ev.Killed) != TraitorsOrganization)
            return;

        TryConvert();
    }

    private void OnSecretIdentityChanged(ref ESSecretIdentityChangedEvent ev)
    {
        // only check when identity is being removed
        if (ev.NewSecretIdentity != null || ev.OldSecretIdentity == null)
            return;

        // only convert if their old one was traitor
        if (ev.OldSecretIdentity.Organization != TraitorsOrganization)
            return;

        TryConvert();
    }

    private void TryConvert()
    {
        if (!_masquerade.TryGetMasqueradeData(out var set))
            return; // Well, no masquerade means no conversion target.

        if (set.SuperfanTarget is not { } entry)
        {
            // Fail silently, we were never configured to begin with. See #1079
            return;
        }

        var total = 0;
        var dead = 0;
        foreach (var member in _secretIdentity.GetOrganizationMembers(TraitorsOrganization))
        {
            total += 1;

            if (_mind.IsCharacterDeadIc(Comp<MindComponent>(member)))
                dead += 1;
        }

        // Chance to be converted is proportional to the number of dead organization members.
        var prob = total != 0
            ? (float)dead / total
            : 1;

        var fanQuery = EntityQueryEnumerator<ESSuperfanComponent, MindComponent>();
        while (fanQuery.MoveNext(out var ent, out _, out var mind))
        {
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

            _secretIdentity.ChangeSecretIdentity((ent, mind), entry.PickSecretIdentities(_random, _proto).Single());
        }
    }
}
