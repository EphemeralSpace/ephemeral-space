using Content.Server._ES.Armory.Components;
using Content.Server.Chat.Systems;
using Content.Server.Doors.Systems;
using Content.Server.Electrocution;
using Content.Server.GameTicking.Rules;
using Content.Shared._ES.Core.Timer;
using Content.Shared.Doors.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Robust.Shared.Timing;

namespace Content.Server._ES.Armory;

/// <summary>
///     Handles the armory opening sequence which is intended to go as follows
///     - N amount of buttons distributed in whatever way
///     - different players must press each button within some timeframe
///     - if this is done successfully, the armory opens after some delay with an announcement
///     - if this is not done successfully, everyone in the area is electrocuted (with no way to tell which buttons werent pressed)
///       with an announcement, and the buttons cannot be pressed again for a certain amount of time
/// </summary>
// todo test this behavior should be easily testable more or less
public sealed class ESArmorySystem : GameRuleSystem<ESArmoryGameRuleComponent>
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DoorSystem _door = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ESEntityTimerSystem _timer = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESArmoryGovernanceInterfaceComponent, ActivateInWorldEvent>(OnActivateInWorld);
    }

    protected override void ActiveTick(EntityUid uid, ESArmoryGameRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        // nothing more to check if its already opened
        if (component.Opened)
            return;

        // check for cooldown stuff
        if (component.ArmoryCooldownLiftsAt is not null &&
            _timing.CurTime < component.ArmoryCooldownLiftsAt)
        {
            return;
        }

        component.ArmoryCooldownLiftsAt = null;

        // check to see if we should fail or succeed the sequence
        var query = EntityQueryEnumerator<ESArmoryGovernanceInterfaceComponent>();
        var failed = false;
        var anyNotPressed = false;
        while (query.MoveNext(out _, out var button))
        {
            if (button.ButtonPressedAt is null)
            {
                anyNotPressed = true;
            }
            // fail the sequence if we go past the leeway time
            else if (_timing.CurTime > (button.ButtonPressedAt.Value + component.ButtonPressAllTimeframe))
            {
                failed = true;
                break;
            }
        }

        // fail if past time, succeed if not past time & all are pressed
        if (failed)
            FailArmory(component);
        else if (!anyNotPressed)
            StartArmoryOpenTimer(component);
    }

    // todo jank replace with actual singleton instead of gamerule stuff
    private ESArmoryGameRuleComponent? GetArmorySingleton()
    {
        ESArmoryGameRuleComponent? armory;
        var query = EntityQueryEnumerator<ESArmoryGameRuleComponent>();
        while (query.MoveNext(out _, out armory))
        {
            break;
        }

        return armory;
    }

    private void OnActivateInWorld(Entity<ESArmoryGovernanceInterfaceComponent> ent, ref ActivateInWorldEvent args)
    {
        if (HasComp<ESArmoryPressedButtonAlreadyComponent>(args.User))
            return;

        if (!GameTicker.IsGameRuleActive<ESArmoryGameRuleComponent>() || GetArmorySingleton() is not { } armory)
            return;

        // no need to check cooldown time since the update loop will handle that stuff, just check if theres any cooldown
        if (armory.Opened || armory.ArmoryCooldownLiftsAt is not null)
            return;

        if (ent.Comp.ButtonPressedAt is not null)
            return;

        EnsureComp<ESArmoryPressedButtonAlreadyComponent>(args.User);
        ent.Comp.ButtonPressedAt = _timing.CurTime;

        TrySetArmoryControlRoomDoorBolt(true);
    }

    #region Armory control

    public void StartArmoryOpenTimer(ESArmoryGameRuleComponent component)
    {
        // bookkeeping
        component.Opened = true;
        ResetArmoryButtonUsers();
        TrySetArmoryControlRoomDoorBolt(false);

        // Announcement
        _chat.DispatchGlobalAnnouncement(
            Loc.GetString("es-armory-opening-announcement"),
            Loc.GetString("es-armory-announcer"),
            false,
            null,
            Color.Coral);

        // start open timer
        _ = _timer.SpawnMethodTimer(component.ArmoryOpenDelay, OpenArmory);
    }

    private void OpenArmory()
    {
        var query = EntityQueryEnumerator<ESArmoryDoorComponent, DoorComponent, DoorBoltComponent>();
        while (query.MoveNext(out var uid, out var armory, out var door, out var bolt))
        {
            _door.TryOpenAndBolt(uid, door);
        }

        // Announcement
        _chat.DispatchGlobalAnnouncement(
            Loc.GetString("es-armory-opened-announcement"),
            Loc.GetString("es-armory-announcer"),
            false,
            null,
            Color.Coral);
    }

    // Fail army #FailArmyNation
    public void FailArmory(ESArmoryGameRuleComponent component)
    {
        // bookkeeping
        component.ArmoryCooldownLiftsAt = _timing.CurTime + component.ArmoryCooldownTime;
        ResetArmoryButtonUsers();
        TrySetArmoryControlRoomDoorBolt(false);

        // Stun everyone in radius of any button
        HashSet<Entity<MobStateComponent>> targets = new();
        var query = EntityQueryEnumerator<ESArmoryGovernanceInterfaceComponent, TransformComponent>();
        while (query.MoveNext(out _, out _, out var xform))
        {
            // todo this should be scoping off of a better component than this but whatever.
            var ents = _lookup.GetEntitiesInRange<MobStateComponent>(xform.Coordinates, 3f);
            targets.UnionWith(ents);
        }

        foreach (var ent in targets)
        {
            _electrocution.TryDoElectrocution(ent, null, 5, component.FailWritheDuration, true, ignoreInsulation: true);
        }

        // Announcement
        _chat.DispatchGlobalAnnouncement(
            Loc.GetString("es-armory-failed-to-open-announcement"),
            Loc.GetString("es-armory-announcer"),
            false,
            null,
            Color.Coral);
    }

    private void ResetArmoryButtonUsers()
    {
        var query = EntityQueryEnumerator<ESArmoryPressedButtonAlreadyComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            RemCompDeferred<ESArmoryPressedButtonAlreadyComponent>(uid);
        }
    }

    private void TrySetArmoryControlRoomDoorBolt(bool toBolt)
    {
        var query = EntityQueryEnumerator<ESArmoryControlRoomDoorComponent, DoorComponent, DoorBoltComponent>();
        while (query.MoveNext(out var uid, out _, out var door, out var doorBolt))
        {
            if (_door.IsBolted(uid, doorBolt) == toBolt)
                continue;

            _door.TryClose(uid, door);
            _door.TrySetBoltDown((uid, doorBolt), toBolt);
        }
    }
    #endregion
}
