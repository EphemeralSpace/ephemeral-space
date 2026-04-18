using Content.Server._ES.WarpDrive.Components;
using Content.Server.GameTicking.Rules;
using Content.Shared.GameTicking.Components;

namespace Content.Server._ES.WarpDrive;

/// <summary>
///     Handles all warp drive behavior
/// </summary>
/// <see cref="ESWarpDriveGameRuleComponent"/>
public sealed partial class ESWarpDriveSystem : GameRuleSystem<ESWarpDriveGameRuleComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        InitializeSingularityWorld();
    }

    protected override void Started(EntityUid uid,
        ESWarpDriveGameRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        StartedSingularityWorld(component);
    }

    protected override void ActiveTick(EntityUid uid, ESWarpDriveGameRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);
    }
}
