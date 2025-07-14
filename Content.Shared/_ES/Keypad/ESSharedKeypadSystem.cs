using Content.Shared._ES.Keypad.Components;
using Content.Shared.Audio;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Keypad;

public abstract class ESSharedKeypadSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESKeypadComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ESKeypadComponent, ESToggleEntryModeDoAfterEvent>(OnToggleEntryModeDoAfter);
        SubscribeLocalEvent<ESKeypadComponent, ExaminedEvent>(OnExamined);

        SubscribeAllEvent<ESKeypadPressKeyEvent>(OnKeypadPressed);
        SubscribeAllEvent<ESKeypadClearEvent>(OnKeypadClear);
        SubscribeAllEvent<ESKeypadSubmitEvent>(OnKeypadSubmit);
    }

    private void OnInteractUsing(Entity<ESKeypadComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.Locked)
            return;

        var ev = new ESToggleEntryModeDoAfterEvent();
        args.Handled = _tool.UseTool(args.Used, args.User, ent, 0, ent.Comp.EditModeQuality, ev);
    }

    private void OnToggleEntryModeDoAfter(Entity<ESKeypadComponent> ent, ref ESToggleEntryModeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        ent.Comp.EditModeEnabled = !ent.Comp.EditModeEnabled;
        Dirty(ent);
        args.Handled = true;
    }

    private void OnExamined(Entity<ESKeypadComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(ESKeypadComponent), 1))
        {
            var edit = ent.Comp.EditModeEnabled && args.IsInDetailsRange;
            var tool = _prototype.Index(ent.Comp.EditModeQuality);

            if (_powerReceiver.IsPowered(ent.Owner))
                args.PushMarkup(Loc.GetString("es-keypad-examine", ("locked", ent.Comp.Locked), ("edit", edit)));
            if (!ent.Comp.Locked && args.IsInDetailsRange)
                args.PushMarkup(Loc.GetString("es-keypad-examine-edit-mode", ("tool", Loc.GetString(tool.ToolName))));
        }
    }

    private void OnKeypadPressed(ESKeypadPressKeyEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } player)
            return;

        if (!TryGetEntity(ev.Keypad, out var keypad) ||
            !TryComp<ESKeypadComponent>(keypad, out var keypadComponent))
            return;

        InputKey((keypad.Value, keypadComponent), ev.Key, player);
    }

    private void OnKeypadClear(ESKeypadClearEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } player)
            return;

        if (!TryGetEntity(ev.Keypad, out var keypad) ||
            !TryComp<ESKeypadComponent>(keypad, out var keypadComponent))
            return;

        ClearKeypad((keypad.Value, keypadComponent), player);
    }

    private void OnKeypadSubmit(ESKeypadSubmitEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } player)
            return;

        if (!TryGetEntity(ev.Keypad, out var keypad) ||
            !TryComp<ESKeypadComponent>(keypad, out var keypadComponent))
            return;

        ToggleLock((keypad.Value, keypadComponent), player);
    }

    public void InputKey(Entity<ESKeypadComponent> ent, char key, EntityUid? user = null)
    {
        if (!_powerReceiver.IsPowered(ent.Owner))
            return;

        if (user.HasValue)
        {
            // Lifted gratuitously from NukeSystem
            var semitoneShift = key.ToString() switch
            {
                "1" => 0,
                "2" => 2,
                "3" => 3,
                "4" => 4,
                "5" => 5,
                "6" => 6,
                "7" => 7,
                "8" => 9,
                "9" => 10,
                "0" => 12,
                _ => 0
            };

            _audio.PlayPredicted(ent.Comp.KeypadPressSound,
                ent.Owner,
                user,
                AudioHelpers.ShiftSemitone(ent.Comp.KeypadPressSound.Params, semitoneShift));
        }

        if (ent.Comp.CodeInput.Length >= ent.Comp.CodeLength)
            return;

        ent.Comp.CodeInput = $"{ent.Comp.CodeInput}{key}";
        Dirty(ent);
    }

    public void ClearKeypad(Entity<ESKeypadComponent> ent, EntityUid? user = null)
    {
        if (!_powerReceiver.IsPowered(ent.Owner))
            return;

        if (user.HasValue)
            _audio.PlayPredicted(ent.Comp.KeypadPressSound, ent.Owner, user);

        if (ent.Comp.CodeInput.Length == 0)
            return;

        ent.Comp.CodeInput = string.Empty;
        Dirty(ent);
    }

    public void ToggleLock(Entity<ESKeypadComponent> ent, EntityUid? user = null)
    {
        if (!_powerReceiver.IsPowered(ent.Owner))
            return;

        if (ent.Comp.CodeInput.Length != ent.Comp.CodeLength)
        {
            if (user.HasValue)
                _audio.PlayPredicted(ent.Comp.KeypadPressSound, ent.Owner, user);
            return;
        }

        // Change passcode
        if (ent.Comp.EditModeEnabled)
        {
            ent.Comp.Passcode = ent.Comp.CodeInput;
            ent.Comp.CodeInput = string.Empty;
            _audio.PlayPredicted(ent.Comp.RightCodeSound, ent, user, ent.Comp.RightCodeSound.Params.WithPitchScale(1.15f));
            _popup.PopupPredicted(Loc.GetString("es-keypad-popup-code-changed"), ent, user);
            Dirty(ent);
            return;
        }

        // Check correct code
        if (!string.IsNullOrWhiteSpace(ent.Comp.Passcode) && ent.Comp.CodeInput != ent.Comp.Passcode)
        {
            _audio.PlayPredicted(ent.Comp.WrongCodeSound, ent, user);
            ent.Comp.CodeInput = string.Empty;
            Dirty(ent);
            return;
        }

        ent.Comp.CodeInput = string.Empty;
        ent.Comp.Locked = !ent.Comp.Locked;
        _audio.PlayPredicted(ent.Comp.RightCodeSound, ent, user);
        _appearance.SetData(ent.Owner, ESKeypadVisuals.Locked, ent.Comp.Locked);
        _pointLight.SetColor(ent, ent.Comp.Locked ? ent.Comp.LockedColor : ent.Comp.UnlockedColor);
        Dirty(ent);
        OnLockToggled(ent);
    }

    protected virtual void OnLockToggled(Entity<ESKeypadComponent> ent)
    {

    }
}
