using System.Linq;
using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Presets;
using Content.Shared._Citadel.Utilities;
using Content.Shared._ES.SecretIdentity;
using Content.Shared._ES.SecretIdentity.Masquerades;
using Content.Shared.Administration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Toolshed;

namespace Content.Server._ES.SecretIdentity.Masquerades;

[ToolshedCommand(Name = "mq")]
[AdminCommand(AdminFlags.Round )]
public sealed partial class MasqueradeCommands : ToolshedCommand
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;

    public static readonly ProtoId<GamePresetPrototype> MasqueradePreset = "ESMasqueradeManaged";

    [CommandImplementation("pickFromSecretIdentitySet")]
    public List<ProtoId<ESSecretIdentityPrototype>> PickFromSecretIdentitySet(
        [CommandArgument] ProtoId<ESSecretIdentitySetPrototype> secretIdentitySet,
        [CommandArgument] RngSeed seed,
        [CommandArgument] int count
        )
    {
        var rng = seed.IntoRandomizer();

        var set = _proto.Index(secretIdentitySet);

        return set.Pick(rng, count);
    }

    [CommandImplementation("sim")]
    public IEnumerable<string> Simulate(
        [CommandArgument] ProtoId<ESMasqueradePrototype> masquerade,
        [CommandArgument] int playerCount,
        [CommandArgument] int trials
        )
    {
        var proto = _proto.Index(masquerade);

        var allSecretIdentities = new List<(ESSecretIdentityPrototype SecretIdentity, int Count)>();
        for (var i = 0; i < trials; ++i)
        {
            if (!proto.Masquerade.TryGetSecretIdentities(playerCount, _random, _proto, out var secretIdentities))
                throw new Exception($"Failed to get secret identities for masquerade {masquerade} at pop level {playerCount}");

            foreach (var grouping in secretIdentities.GroupBy(m => m))
            {
                allSecretIdentities.Add((_proto.Index(grouping.Key), grouping.Count()));
            }
        }

        var ordered = allSecretIdentities
            .GroupBy(n => n.SecretIdentity)
            .OrderByDescending(p => p.Key.Organization)
            .ThenByDescending(p => (double) p.Sum(g => g.Count) / trials)
            .ThenBy(p => p.Key.ID);

        foreach (var grouping in ordered)
        {
            var mean = (double) grouping.Sum(g => g.Count) / trials;
            var sum = grouping.Sum(d => Math.Pow(d.Count - mean, 2));
            var stdDev = Math.Sqrt(sum / trials);

            yield return $"{grouping.Key.ID} ({grouping.Key.Organization}): μ={mean:F4}, σ={stdDev:F4}";
        }
    }

    [CommandImplementation("force")]
    public void ForceMasquerade([CommandArgument] ProtoId<ESMasqueradePrototype> masquerade)
    {
        var mqSys = Sys<ESMasqueradeSystem>();
        var gameTicker = Sys<GameTicker>();

        mqSys.ForceMasquerade(masquerade);
        gameTicker.SetGamePreset(MasqueradePreset);
    }

    // exists due to toolshed and C# limitations around nulls.
    [CommandImplementation("unforce")]
    public void UnforceMasquerade()
    {
        var mqSys = Sys<ESMasqueradeSystem>();

        mqSys.ForceMasquerade(null);
    }
}
