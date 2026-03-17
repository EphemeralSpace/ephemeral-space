using System.Collections.Generic;
using Content.IntegrationTests.Tests._Citadel.Attributes;
using Robust.Shared.Configuration;

namespace Content.IntegrationTests.Tests._Citadel;

public abstract partial class GameTest
{
    [SidedDependency(Side.Server)] private readonly IConfigurationManager _serverCfg = default!;
    [SidedDependency(Side.Client)] private readonly IConfigurationManager _clientCfg = default!;

    private readonly Dictionary<string, object> _clientCVarOverrides = new();
    private readonly Dictionary<string, object> _clientOldCVarValues = new();
    private readonly Dictionary<string, object> _serverCVarOverrides = new();
    private readonly Dictionary<string, object> _serverOldCVarValues = new();

    /// <summary>
    ///     Adds a setup-time override for a given cvar, for use by <see cref="IGameTestModifier"/>s.
    /// </summary>
    public void PreTestAddOverride(Side side, string cVar, object value)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (Pair is null)
            throw new NotSupportedException("Cannot use PreTest functions after test SetUp.");

        if (side is Side.Neither)
            throw new NotSupportedException($"Must specify a side, or both, for {nameof(PreTestAddOverride)}");

        if ((side & Side.Server) != 0)
            _serverCVarOverrides.Add(cVar, value);

        if ((side & Side.Client) != 0)
            _clientCVarOverrides.Add(cVar, value);
    }

    private async Task DoPreTestOverrides()
    {
        foreach (var (cvar, value) in _clientCVarOverrides)
        {
            await OverrideCVarByName(Side.Client, cvar, value, false);
        }

        foreach (var (cvar, value) in _serverCVarOverrides)
        {
            await OverrideCVarByName(Side.Server, cvar, value, false);
        }

        await RunUntilSynced();
    }

    /// <summary>
    ///     Caches a CVar in one of the OldCVarValues dicts for restoration later.
    /// </summary>
    private void StoreCVar(Side side, string cvar)
    {
        if (side is Side.Client)
        {
            // We store only if this is our first time overriding.
            if (!_clientOldCVarValues.ContainsKey(cvar))
                _clientOldCVarValues[cvar] = _clientCfg.GetCVar(cvar);
        }
        else if (side is Side.Server)
        {
            // We store only if this is our first time overriding.
            if (!_serverOldCVarValues.ContainsKey(cvar))
                _serverOldCVarValues[cvar] = _serverCfg.GetCVar(cvar);
        }
    }

    /// <summary>
    ///     Sets a given CVar for the provided side.
    /// </summary>
    /// <remarks>Does its own cleanup, you do not need to and should not set the CVar back yourself.</remarks>
    public async Task OverrideCVar<T>(Side side, CVarDef<T> cvar, T value, bool sync = true)
    {
        await OverrideCVarByName(side, cvar.Name, value, sync);
    }

    private async Task OverrideCVarByName(Side side, string cVar, object value, bool sync)
    {
        StoreCVar(side, cVar);

        if (side is Side.Client)
        {
            _clientCfg.SetCVar(cVar, value);
        }
        else if (side is Side.Server)
        {
            _serverCfg.SetCVar(cVar, value);
        }
        else
        {
            throw new NotSupportedException($"Expected a specific side, got {side}.");
        }

        if (sync)
            await RunUntilSynced();
    }

    private void RestoreCVars()
    {
        foreach (var (key, value) in _clientOldCVarValues)
        {
            _clientCfg.SetCVar(key, value);
        }

        foreach (var (key, value) in _serverOldCVarValues)
        {
            _serverCfg.SetCVar(key, value);
        }
    }
}
