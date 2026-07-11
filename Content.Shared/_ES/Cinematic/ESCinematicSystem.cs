using Content.Shared.Atmos.Piping;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Cinematic;

/// <summary>
///     Contains the API for playing a cinematic for the given clients.
/// </summary>
public sealed partial class ESCinematicSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;

    /// <summary>
    ///     Plays a cinematic for the given clients.
    /// </summary>
    /// <remarks>
    ///     If this is called from clientside, any given filter will be ignored and the cutscene played for your own client.
    /// </remarks>
    /// <param name="cinematic">The cinematic to play</param>
    /// <param name="filter">A filter containing the clients that should play the cinematic. Defaults to local on client, or empty on server.</param>
    public void PlayCinematic(ProtoId<ESCinematicPrototype> cinematic, Filter? filter = null)
    {
        filter ??= Filter.Empty();

        // Playing a cinematic from the client will always raise locally regardless of the filter.
        if (_net.IsClient)
        {
            RaiseLocalEvent(new PlayCinematicEvent { Cinematic = cinematic });
            return;
        }

        RaiseNetworkEvent(new PlayCinematicEvent { Cinematic = cinematic }, filter);
    }
}
