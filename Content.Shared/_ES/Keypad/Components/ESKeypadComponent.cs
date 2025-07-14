using Content.Shared.DeviceLinking;
using Content.Shared.DoAfter;
using Content.Shared.Tools;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Keypad.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(ESSharedKeypadSystem))]
public sealed partial class ESKeypadComponent : Component
{
    [DataField]
    public int CodeLength = 4;

    [DataField, AutoNetworkedField]
    public string? Passcode;

    [DataField, AutoNetworkedField]
    public string CodeInput = string.Empty;

    [DataField, AutoNetworkedField]
    public bool Locked;

    [DataField]
    public ProtoId<ToolQualityPrototype> EditModeQuality = "Screwing";

    [DataField, AutoNetworkedField]
    public bool EditModeEnabled;

    [DataField]
    public SoundSpecifier KeypadPressSound = new SoundPathSpecifier("/Audio/Machines/Nuke/general_beep.ogg")
    {
        Params = new AudioParams
        {
            Volume = -5,
        },
    };

    [DataField]
    public SoundSpecifier WrongCodeSound = new SoundPathSpecifier("/Audio/Effects/beep1.ogg")
    {
        Params = new AudioParams
        {
            Pitch = 0.5f,
        },
    };

    [DataField]
    public SoundSpecifier RightCodeSound = new SoundPathSpecifier("/Audio/Effects/beep1.ogg");

    [DataField]
    public Color UnlockedColor = Color.FromHex("#33ff33");

    [DataField]
    public Color LockedColor = Color.FromHex("#f31818");

    [DataField]
    public ProtoId<SourcePortPrototype> LockPort = "ESKeypadLock";

    [DataField]
    public ProtoId<SourcePortPrototype> UnlockPort = "ESKeypadUnlock";
}

[Serializable, NetSerializable]
public sealed partial class ESToggleEntryModeDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}

[UsedImplicitly]
[Serializable, NetSerializable]
public enum ESKeypadUiKey : byte
{
    Key,
}

[UsedImplicitly]
[Serializable, NetSerializable]
public enum ESKeypadVisuals : byte
{
    Locked,
}

[Serializable, NetSerializable]
public sealed class ESKeypadPressKeyEvent(NetEntity keypad, char key) : EntityEventArgs
{
    public NetEntity Keypad = keypad;
    public char Key = key;
}

[Serializable, NetSerializable]
public sealed class ESKeypadClearEvent(NetEntity keypad) : EntityEventArgs
{
    public NetEntity Keypad = keypad;
}

[Serializable, NetSerializable]
public sealed class ESKeypadSubmitEvent(NetEntity keypad) : EntityEventArgs
{
    public NetEntity Keypad = keypad;
}


