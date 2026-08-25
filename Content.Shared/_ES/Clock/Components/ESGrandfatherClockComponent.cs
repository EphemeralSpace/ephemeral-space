using Content.Shared._ES.Core.Timer.Components;
using Content.Shared.Clock;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Clock.Components;

/// <summary>
/// <see cref="ClockComponent"/> that does chimes at every hour.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESGrandfatherClockSystem))]
public sealed partial class ESGrandfatherClockComponent : Component
{
    [DataField, AutoNetworkedField]
    public int? LastHour;

    [DataField]
    public SoundSpecifier MelodySound = new SoundPathSpecifier("/Audio/_ES/Effects/westminster.ogg")
    {
        Params = new AudioParams
        {
            Volume = 3,
        },
    };

    [DataField]
    public SoundSpecifier ChimeSound = new SoundPathSpecifier("/Audio/_ES/Effects/chime.ogg")
    {
        Params = new AudioParams
        {
            Volume = 3,
            Variation = 0.01f,
        },
    };

    [DataField]
    public TimeSpan MelodyDelay = TimeSpan.FromSeconds(7f);

    [DataField]
    public TimeSpan ChimeDelay = TimeSpan.FromSeconds(2.5f);
}

[Serializable, NetSerializable]
public sealed partial class ESGrandfatherClockChimeTimerEvent : ESEntityTimerEvent;
