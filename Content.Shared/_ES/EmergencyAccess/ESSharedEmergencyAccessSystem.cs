using System.Diagnostics.CodeAnalysis;
using Content.Shared._ES.EmergencyAccess.Components;
using Content.Shared.Examine;
using Robust.Shared.Random;

namespace Content.Shared._ES.EmergencyAccess;

public sealed class ESSharedEmergencyAccessSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESEmergencyAccessDoorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ESEmergencyAccessDoorComponent, ExaminedEvent>(OnExamined);
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
                if (comp.Key != key)
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
