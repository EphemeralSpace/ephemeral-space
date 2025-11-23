using Content.Server._ES.Door.Components;
using Content.Server.Doors.Systems;
using Content.Server.GameTicking;
using Content.Shared.Doors.Components;

namespace Content.Server._ES.Door;

/// <summary>
/// This handles <see cref="ESRunLevelDoorComponent"/>
/// </summary>
public sealed class ESRunLevelDoorSystem : EntitySystem
{
    [Dependency] private readonly DoorSystem _door = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        var query = EntityQueryEnumerator<ESRunLevelDoorComponent, DoorComponent>();
        while (query.MoveNext(out var uid, out var comp, out var door))
        {
            var open = (ev.New == comp.OpenRunLevel) ^ comp.Invert;

            if (open)
            {
                _door.TryOpen(uid, door, quiet: true);
            }
            else
            {
                _door.TryClose(uid, door);
            }
        }
    }
}
