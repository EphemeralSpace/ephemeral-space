using Content.Server._ES.SecretIdentity.Objectives.Relays.Components;
using Content.Server._ES.WarpDrive;
using Content.Server.Mind;
using Content.Shared._ES.Mind;

namespace Content.Server._ES.SecretIdentity.Objectives.Relays;

public sealed partial class ESWarpUnclogRelaySystem : ESBaseMindRelay
{
    [Dependency] private MindSystem _mind = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ESWarpUnclogRelayComponent, WarpDriveInterruptionClearedEvent>(OnInterruptionCleared);
    }


    private void OnInterruptionCleared(Entity<ESWarpUnclogRelayComponent> ent, ref WarpDriveInterruptionClearedEvent args)
    {
        if (!_mind.TryGetMind(ent, out var mindId, out var mindComp))
            return;

        var ev = new ESWarpDriveInterruptionClearedEvent();
        RaiseMindEvent((mindId, mindComp), ref ev);
    }
}

[ByRefEvent]
public readonly record struct ESWarpDriveInterruptionClearedEvent();


