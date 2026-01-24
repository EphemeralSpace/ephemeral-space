using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Content.IntegrationTests.Tests._Citadel;
using Content.IntegrationTests.Tests._Citadel.Constraints;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.Core.Timer.Components;
using Robust.Server.GameStates;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;

namespace Content.IntegrationTests.Tests._ES.Timers;

[TestFixture]
public sealed class TimerTests : GameTest
{
    // Just uses the pool instead of trying to spin up reflectionmanager standalone, as we already have functional
    // pairs floating around in a normal CI testing environment.

    [SidedDependency(Side.Server)] private readonly IReflectionManager _reflection = default!;

    [System(Side.Server)] private readonly ESEntityTimerSystem _sTimer = default!;
    [System(Side.Server)] private readonly PvsOverrideSystem _pvsOverride = default!;

    [Test]
    [TestOf(typeof(ESEntityTimerEvent))]
    [Description("Asserts that all timer events are marked appropriately for their side.")]
    [RunOnSide(Side.Server)]
    public void EnsureTimerEventSanity()
    {
        using (Assert.EnterMultipleScope())
        {
            foreach (var type in _reflection.GetAllChildren<ESEntityTimerEvent>())
            {
                var side = GetSideOfType(type);

                switch (side)
                {
                    case Side.Client:
                    case Side.Server:
                        Assert.That(type, Has.No.Attribute<NetSerializableAttribute>());
                        break;
                    case Side.Neither: // Shared
                        Assert.That(type, Has.Attribute<NetSerializableAttribute>().Or.Attribute<NonNetworkedTimerEventAttribute>());
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }

    [Test]
    [TestOf(typeof(ESEntityTimerSystem))]
    [Description("Ensures shared timers synchronize over the network correctly.")]
    public async Task EnsureTimerSync()
    {
        Entity<ESEntityTimerComponent>? timer = null;

        await Server.WaitAssertion(() =>
        {
            timer = _sTimer.SpawnTimer(TimeSpan.FromSeconds(1), new TestTimerEvent());
            Assert.That(timer, Is.MapInitialized(Server));

            Assert.That(timer!.Value.Comp.TimerEndEvent, Is.TypeOf<TestTimerEvent>());

            _pvsOverride.AddGlobalOverride(timer.Value); // Ensure it gets synced.
        });

        await Pair.RunTicksSync(10);

        await Client.WaitAssertion(() =>
        {
            var ctimer = CEntity<ESEntityTimerComponent>(ToClientUid(timer!.Value));

            Assert.That(ctimer.Comp.TimerEndEvent, Is.TypeOf<TestTimerEvent>());

        });

        await Pair.RunSeconds(3);

        await Server.WaitAssertion(() =>
        {
            Assert.That(timer, Is.Deleted(Server), "Timer should have expired.");
        });
    }

    [Test]
    [TestOf(typeof(ESEntityTimerSystem))]
    [Description("Ensures method timers, and other non-sync timers, do NOT try to send unsupported data over the network.")]
    public async Task EnsureMethodTimerSync()
    {
        Entity<ESEntityTimerComponent>? timer = null;

        var ran = false;

        await Server.WaitAssertion(() =>
        {
            timer = _sTimer.SpawnMethodTimer(TimeSpan.FromSeconds(1), () => ran = true);
            Assert.That(timer, Is.MapInitialized(Server));

            Assert.That(timer!.Value.Comp.TimerEndEvent, Is.TypeOf<MethodTimerEvent>());

            _pvsOverride.AddGlobalOverride(timer.Value); // Ensure it gets synced.
        });

        await Pair.RunTicksSync(10);

        await Client.WaitAssertion(() =>
        {
            var ctimer = ToClientUid(timer!.Value);

            Assert.That(ctimer, Has.No.Comp<ESEntityTimerComponent>(Client));
        });

        await Pair.RunSeconds(3);

        await Server.WaitAssertion(() =>
        {
            Assert.That(timer, Is.Deleted(Server), "Timer should have expired.");
        });

        Assert.That(ran, Is.True, "Method timer should've ran by now.");
    }


    // TODO(Kaylie): This might be useful as some smarter, global helper somewhere.
    //               But right now it's fine here.
    //               Also it's not threadsafe but this fixture type doesn't need that.

    private readonly Dictionary<Assembly, Side> _assembliesToSide = new();

    private Side GetSideOfType(Type t)
    {
        return GetSideOfAssembly(t.Assembly);
    }

    private Side GetSideOfAssembly(Assembly a)
    {
        if (_assembliesToSide.TryGetValue(a, out var side))
            return side;

        // The engine already knows all these, but it doesn't tell them to us. Shame.
        foreach (var entrypoint in a.ExportedTypes.Where(x => x.IsAssignableTo(typeof(GameShared))))
        {
            if (entrypoint.IsAssignableTo(typeof(GameClient)))
            {
                _assembliesToSide.Add(a, Side.Client);
                return Side.Client;
            }
            else if (entrypoint.IsAssignableTo(typeof(GameServer)))
            {
                _assembliesToSide.Add(a, Side.Server);
                return Side.Server;
            }
            else
            {
                _assembliesToSide.Add(a, Side.Neither);
                return Side.Neither;
            }
        }

        _assembliesToSide.Add(a, Side.Neither);
        return Side.Neither;
    }
}
