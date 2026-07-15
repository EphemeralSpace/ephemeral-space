using Content.Server._ES.StationVariation.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.VariationPass;
using Content.Shared._ES.Camera;

namespace Content.Server._ES.StationVariation.Systems;

public sealed class ESRotatedCameraGameRuleSystem : VariationPassSystem<ESRotatedCameraGameRuleComponent>
{
    protected override void ApplyVariation(Entity<ESRotatedCameraGameRuleComponent> ent, ref StationVariationPassEvent args)
    {
        var maps = new HashSet<EntityUid?>();
        foreach (var grid in args.Station.Comp.Grids)
        {
            maps.Add(Transform(grid).MapUid);
        }

        foreach (var map in maps)
        {
            // erm
            if (map is null)
                continue;

            var comp = EnsureComp<ESMapCameraRotationOverrideComponent>(map.Value);
            comp.RotationOverride += ent.Comp.Angle;
        }
    }
}
