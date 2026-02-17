#nullable enable
using System.Collections.Generic;
using Content.Client.IoC;
using Content.Client.Parallax.Managers;
using Content.IntegrationTests.Tests.Destructible;
using Content.IntegrationTests.Tests.DeviceNetwork;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.Players;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Pair;

/// <summary>
/// This object wraps a pooled server+client pair.
/// </summary>
public sealed partial class TestPair : RobustIntegrationTest.TestPair
{
    private List<NetUserId> _modifiedProfiles = new();

    public ContentPlayerData? PlayerData => Player?.Data.ContentData();

    protected override async Task Initialize()
    {
        await base.Initialize();

        // Prevent info log spam in some tests (particularly SpawnAndDeleteAllEntitiesOnDifferentMaps)
        Server.System<SharedMapSystem>().Log.Level = LogLevel.Warning;
        Client.EntMan.EntitySysManager.SystemLoaded += (_, e) =>
        {
            if (e.System is SharedMapSystem map)
                map.Log.Level = LogLevel.Warning;
        };

        // ES EDIT: Always reset the sim world during tests. This makes them slightly slower but prevents flapping tests
        //          from blowing shit up for every test downstream of them too easily.
        var gameTicker = Server.System<GameTicker>();
        await Server.WaitPost(() => gameTicker.RestartRound());
        // END ES EDIT
    }

    public override async Task RevertModifiedCvars()
    {
        // I just love order dependent cvars
        // I.e., cvars that when changed automatically cause others to also change.
        var modified = ModifiedServerCvars.TryGetValue(CCVars.PanicBunkerEnabled.Name, out var panik);

        await base.RevertModifiedCvars();

        if (!modified)
            return;

        await Server.WaitPost(() => Server.CfgMan.SetCVar(CCVars.PanicBunkerEnabled.Name, panik!));
        ClearModifiedCvars();
    }

    protected override async Task ApplySettings(IIntegrationInstance instance, PairSettings n)
    {
        var next = (PoolSettings)n;
        await base.ApplySettings(instance, next);
        var cfg = instance.CfgMan;
        await instance.WaitPost(() =>
        {
            if (cfg.IsCVarRegistered(CCVars.GameDummyTicker.Name))
                cfg.SetCVar(CCVars.GameDummyTicker, next.DummyTicker);

            if (cfg.IsCVarRegistered(CCVars.GameLobbyEnabled.Name))
                cfg.SetCVar(CCVars.GameLobbyEnabled, next.InLobby);

            if (cfg.IsCVarRegistered(CCVars.GameMap.Name))
                cfg.SetCVar(CCVars.GameMap, next.Map);

            if (cfg.IsCVarRegistered(CCVars.AdminLogsEnabled.Name))
                cfg.SetCVar(CCVars.AdminLogsEnabled, next.AdminLogsEnabled);
        });

        // ES EDIT: Always reset the sim world during tests. This makes them slightly slower but prevents flapping tests
        //          from blowing shit up for every test downstream of them too easily.
        var gameTicker = Server.System<GameTicker>();
        await Server.WaitPost(() => gameTicker.RestartRound());
        // END ES EDIT
    }

    protected override RobustIntegrationTest.ClientIntegrationOptions ClientOptions()
    {
        var opts = base.ClientOptions();

        opts.LoadTestAssembly = false;
        opts.ContentStart = true;
        opts.FailureLogLevel = LogLevel.Warning;
        opts.Options = new()
        {
            LoadConfigAndUserData = false,
        };

        opts.BeforeStart += () =>
        {
            IoCManager.Resolve<IModLoader>().SetModuleBaseCallbacks(new ClientModuleTestingCallbacks
                {
                    ClientBeforeIoC = () => IoCManager.Register<IParallaxManager, DummyParallaxManager>(true)
                });
            // ES EDIT: Attempt to resolve spam from excessive localization logs.
            IoCManager.Resolve<ILogManager>().GetSawmill("loc").Level = LogLevel.Error;
            // END ES EDIT
        };
        return opts;
    }

    protected override RobustIntegrationTest.ServerIntegrationOptions ServerOptions()
    {
        var opts = base.ServerOptions();

        opts.LoadTestAssembly = false;
        opts.ContentStart = true;
        opts.Options = new()
        {
            LoadConfigAndUserData = false,
        };

        opts.BeforeStart += () =>
        {
            // Server-only systems (i.e., systems that subscribe to events with server-only components)
            // There's probably a better way to do this.
            var entSysMan = IoCManager.Resolve<IEntitySystemManager>();
            entSysMan.LoadExtraSystemType<DeviceNetworkTestSystem>();
            entSysMan.LoadExtraSystemType<TestDestructibleListenerSystem>();
            // ES EDIT: Attempt to resolve spam from excessive localization logs.
            IoCManager.Resolve<ILogManager>().GetSawmill("loc").Level = LogLevel.Error;
            // END ES EDIT
        };
        return opts;
    }
}
