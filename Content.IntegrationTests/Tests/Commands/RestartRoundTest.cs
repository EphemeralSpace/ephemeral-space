using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Commands;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.Commands
{
    [TestFixture]
    [TestOf(typeof(RestartRoundNowCommand))]
    [Ignore("I give up with this test for now. It brokey.")]
    [TrackingIssue("https://github.com/EphemeralSpace/ephemeral-space/issues/1516")]
    public sealed class RestartRoundNowTest : GameTest
    {
        public override PoolSettings PoolSettings => new PoolSettings
        {
            DummyTicker = false,
            Dirty = true
        };

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task RestartRoundAfterStart(bool lobbyEnabled)
        {
            var pair = Pair;
            var server = pair.Server;

            var configManager = server.ResolveDependency<IConfigurationManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var gameTicker = entityManager.System<GameTicker>();

            await pair.RunUntilSynced();

            GameTick tickBeforeRestart = default;

            await server.WaitAssertion(() =>
            {
                Assert.That(configManager.GetCVar(CCVars.GameLobbyEnabled), Is.EqualTo(false));
                configManager.SetCVar(CCVars.GameLobbyEnabled, lobbyEnabled);

                Assert.That(gameTicker.RunLevel, Is.EqualTo(GameRunLevel.InRound));

                tickBeforeRestart = entityManager.CurrentTick;

                gameTicker.RestartRound();

                if (lobbyEnabled)
                {
                    Assert.That(gameTicker.RunLevel, Is.Not.EqualTo(GameRunLevel.InRound));
                }
            });

            await pair.RunTicksSync(60);

            await server.WaitAssertion(() =>
            {
                var tickAfterRestart = entityManager.CurrentTick;

                Assert.That(tickBeforeRestart, Is.LessThan(tickAfterRestart));
            });

            // CVar modification reset happens AFTER recycling.
            configManager.SetCVar(CCVars.GameLobbyEnabled, false);

            await pair.RunUntilSynced();
        }
    }
}
