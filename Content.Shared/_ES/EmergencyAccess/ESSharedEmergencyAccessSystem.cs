using System.Diagnostics.CodeAnalysis;
using Content.Shared._ES.EmergencyAccess.Components;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Examine;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Timing;
using Robust.Shared.Random;

namespace Content.Shared._ES.EmergencyAccess;

public abstract partial class ESSharedEmergencyAccessSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAirlockSystem _airlock = default!;
    [Dependency] private SharedDoorSystem _door = default!;
    [Dependency] private SharedPowerReceiverSystem _powerReceiver = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESEmergencyAccessDoorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ESEmergencyAccessDoorComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ESEmergencyAccessDoorComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ESEmergencyAccessDoorComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<ESEmergencyAccessDoorComponent, DoorBoltsChangedEvent>(OnDoorBoltsChanged);

        Subs.BuiEvents<ESEmergencyAccessConsoleComponent>(ESEmergencyAccessConsoleUiKey.Key,
            subs =>
            {
                subs.Event<ESEmergencyAccessSearchBuiMessage>(OnSearchMessage);
                subs.Event<ESEmergencyAccessToggleBuiMessage>(OnToggleMessage);
            });
    }

    private void OnShutdown(Entity<ESEmergencyAccessDoorComponent> ent, ref ComponentShutdown args)
    {
        var query = AllEntityQuery<ESEmergencyAccessConsoleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!string.Equals(ent.Comp.Key, comp.CurrentKey, StringComparison.InvariantCultureIgnoreCase))
                continue;
            comp.HasValidState = false;
            Dirty(uid, comp);
        }
    }

    private void OnDoorBoltsChanged(Entity<ESEmergencyAccessDoorComponent> ent, ref DoorBoltsChangedEvent args)
    {
        var query = AllEntityQuery<ESEmergencyAccessConsoleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!string.Equals(ent.Comp.Key, comp.CurrentKey, StringComparison.InvariantCultureIgnoreCase))
                continue;
            comp.BoltEnabled = args.BoltsDown;
            Dirty(uid, comp);
        }
    }

    private void OnPowerChanged(Entity<ESEmergencyAccessDoorComponent> ent, ref PowerChangedEvent args)
    {
        var query = AllEntityQuery<ESEmergencyAccessConsoleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!string.Equals(ent.Comp.Key, comp.CurrentKey, StringComparison.InvariantCultureIgnoreCase))
                continue;
            comp.PowerEnabled = args.Powered;
            Dirty(uid, comp);
        }
    }

    private void OnSearchMessage(EntityUid uid, ESEmergencyAccessConsoleComponent component, ESEmergencyAccessSearchBuiMessage args)
    {
        // Cap length
        var key = args.Key.Trim();
        if (key.Length > 5)
            return;

        if (component.CurrentKey == key)
            return;

        component.CurrentKey = key;

        component.HasValidState = TryGetDoorWithKey(component.CurrentKey, out var door);
        if (TryComp<AirlockComponent>(door, out var airlock))
        {
            component.EmergencyEnabled = airlock.EmergencyAccess;
            component.BoltEnabled = _door.IsBolted(door.Value);
            component.PowerEnabled = _powerReceiver.IsPowered(door.Value);
        }

        Dirty(uid, component);
    }

    private void OnToggleMessage(EntityUid uid, ESEmergencyAccessConsoleComponent component, ESEmergencyAccessToggleBuiMessage args)
    {
        if (!_useDelay.TryResetDelay(uid, true))
            return;

        if (!TryGetDoorWithKey(component.CurrentKey, out var door) ||
            !TryComp<AirlockComponent>(door, out var airlock))
            return;

        _airlock.SetEmergencyAccess((door.Value, airlock), !airlock.EmergencyAccess);
        component.EmergencyEnabled = airlock.EmergencyAccess;
        Dirty(uid, component);
    }

    private void OnMapInit(Entity<ESEmergencyAccessDoorComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.Key = GenerateUniqueKey();
        Dirty(ent);
    }

    private void OnExamined(Entity<ESEmergencyAccessDoorComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(ESEmergencyAccessDoorComponent), -1))
        {
            args.PushMarkup(Loc.GetString("es-emergency-access-door-examine", ("key", ent.Comp.Key)));
        }
    }

    private const string KeyLetterPool = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const int KeyMaxDigit = 99;

    private string GenerateUniqueKey()
    {
        var key = string.Empty;

        for (var i = 0; i < 100; ++i)
        {
            var letter = KeyLetterPool[_random.Next(KeyLetterPool.Length)];
            var digit = _random.Next(KeyMaxDigit + 1).ToString("D2");

            key = $"{letter}{digit}";

            // Questionably efficient? At least it's the most logical approach.
            var fail = false;
            var query = AllEntityQuery<ESEmergencyAccessDoorComponent>();
            while (query.MoveNext(out var comp))
            {
                if (!string.Equals(comp.Key, key, StringComparison.InvariantCultureIgnoreCase))
                    continue;
                fail = true;
                break;
            }

            if (!fail)
                return key;
        }

        // Ok i give up. generate some unique bullshit.
        return $"{key}-{_random.Next(0, 10)}";
    }

    /// <summary>
    /// Attempts to retrieve the door with the specified key.
    /// </summary>
    public bool TryGetDoorWithKey(string key, [NotNullWhen(true)] out EntityUid? door)
    {
        door = null;

        var query = EntityQueryEnumerator<ESEmergencyAccessDoorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!string.Equals(comp.Key, key, StringComparison.InvariantCultureIgnoreCase))
                continue;

            door = uid;
            return true;
        }

        return false;
    }
}
