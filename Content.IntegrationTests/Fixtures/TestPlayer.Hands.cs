using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Hands.Systems;
using Content.Shared.Hands.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Fixtures;

public sealed partial class TestPlayer
{
    [SidedDependency(Side.Server)] private readonly HandsSystem _serverHandsSys = default!;

    /// <summary>
    ///     Spawns an entity and has the player pick it up.
    /// </summary>
    /// <param name="proto"></param>
    public async Task SpawnAndPickUp(EntProtoId proto)
    {
        var target = EntityUid.Invalid;

        await _server.WaitPost(() =>
        {
            target = _test.SSpawnAtPosition(proto, _test.SComp<TransformComponent>(SEntity).Coordinates);
        });

        await PickUp(target);
    }

    /// <summary>
    ///     Has the player pick up the given entity.
    /// </summary>
    /// <param name="serverTarget">The server-side entity to pick up.</param>
    public async Task PickUp(EntityUid serverTarget)
    {
        var hands = _test.SComp<HandsComponent>(SEntity);

        if (hands.ActiveHandId == null)
            throw new NotSupportedException("Can't pick up things without an active hand");

        await _server.WaitPost(() =>
        {
            if (!_serverHandsSys.TryForcePickup(SEntity, serverTarget, hands.ActiveHandId, false, false))
                throw new Exception("Failed to actually pick anything up. Fatal to the test.");
        });

        await _test.RunTicksSync(1);
    }
}
