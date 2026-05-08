using Content.Shared._ES.Spawning;
using Robust.Client.Player;

namespace Content.Client._ES.Spawning;

/// <inheritdoc/>
public sealed partial class ESSpawningSystem : ESSharedSpawningSystem
{
    [Dependency] private IPlayerManager _player = default!;

    public TimeSpan GetLocalRespawnTime()
    {
        if (_player.LocalSession == null)
            return TimeSpan.Zero;

        return GetRespawnTime(_player.LocalSession);
    }
}
