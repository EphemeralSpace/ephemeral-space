using Content.Server.Mind;
using Content.Shared.Players;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.IntegrationTests.Fixtures;

public abstract partial class GameTest
{
    /// <summary>
    ///     Assigns the player a body in the test map, ensuring they have a mind as well.
    /// </summary>
    public async Task<EntityUid> AssignPlayerBody(ICommonSession session,
        string playerPrototype = "MobHuman",
        bool godMode = true)
    {
        EntityUid res = default;

        var mindSys = SEntMan.System<MindSystem>();

        await Server.WaitAssertion(() =>
        {
            Assert.That(TestMap,
                Is.Not.Null,
                $"{nameof(AssignPlayerBody)} doesn't work without a {nameof(TestMap)}.");

            // InteractionTest cargo cult.
            mindSys.WipeMind(session.ContentData()?.Mind);

            res = SEntMan.SpawnAtPosition(playerPrototype, TestMap.GridCoords);

            mindSys.ControlMob(session.UserId, res);
        });

        // Sync them back up.
        await Pair.RunTicksSync(5);

        return res;
    }

    public async Task<ICommonSession[]> AddDummySessionsSync(int count = 1)
    {
        var res = await Server.AddDummySessions(count);

        await Pair.ReallyBeIdle(); // That takes a while.

        return res;
    }
}
