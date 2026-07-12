using Content.Shared._ES.Cinematic;
using Robust.Shared.Prototypes;

namespace Content.Client._ES.Cinematic;

/// <summary>
///     Handles subscribing to the play cinematic event and forwarding it to the cinematic UI.
/// </summary>
public sealed class ESClientCinematicSystem : EntitySystem
{
    public Action<ProtoId<ESCinematicPrototype>>? CinematicRequested;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<PlayCinematicEvent>(OnPlayCinematic);
    }

    private void OnPlayCinematic(PlayCinematicEvent ev)
    {
        CinematicRequested?.Invoke(ev.Cinematic);
    }
}
