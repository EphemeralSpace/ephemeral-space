using Content.Server._ES.Stagehand;
using Content.Shared._ES.Masks.Tragedian;
using Content.Shared._ES.Stagehand.Components;
using Content.Shared.Follower;

namespace Content.Server._ES.Masks.Tragedian;

public sealed partial class ESCallStagehandsActionSystem : EntitySystem
{
    [Dependency] private FollowerSystem _follower = default!;
    [Dependency] private ESStagehandNotificationsSystem _stagehandNotifications = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESCallStagehandsInstantAction>(OnCallStagehands);
    }

    private void OnCallStagehands(ESCallStagehandsInstantAction args)
    {
        var query = EntityQueryEnumerator<ESStagehandComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            _follower.StartFollowingEntity(uid, args.Performer);
        }

        _stagehandNotifications.SendStagehandNotification(Loc.GetString("es-tragedian-call-stagehands-announcement",
            ("name", _stagehandNotifications.WrapEntityName(args.Performer))),
            ESStagehandNotificationSeverity.High);

        args.Handled = true;
    }
}
