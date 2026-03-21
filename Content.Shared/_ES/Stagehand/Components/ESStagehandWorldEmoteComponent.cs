using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Stagehand.Components;

/// <summary>
///     Entities with this component can use and always hear stagehand world emotes (cheering and booing atm)
/// </summary>
/// <remarks>
///     This doesn't add the action for any of them, that has to be added with <see cref="ActionGrantComponent"/> or something else
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class ESStagehandWorldEmoteComponent : Component;

/// <summary>
///     Action event for playing a world emote.
/// </summary>
public sealed partial class ESStagehandWorldEmoteEvent : InstantActionEvent
{
    /// <summary>
    ///     Message to send to stagehand chat when this emote is played.
    ///     Passes in the name of the performer as $entity.
    /// </summary>
    [DataField(required: true)]
    public LocId Message;

    [DataField(required: true)]
    public SoundSpecifier Sound;
}
