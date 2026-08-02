using Content.Shared._ES.Cryohusk;
using Content.Shared._ES.Cryohusk.Components;
using Content.Shared._ES.Stagehand;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.SecretIdentity.Cannibal;

public sealed partial class ESCannibalSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private ESSharedCryohuskSystem _cryohusk = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private HungerSystem _hunger = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ESSharedSecretIdentitySystem _secretIdentity = default!;
    [Dependency] private ESSharedStagehandNotificationsSystem _stagehandNotifications = default!;

    private static readonly SoundSpecifier? EatSound = new SoundCollectionSpecifier("ChangelingDevourConsume")
    {
        Params = new AudioParams().WithMaxDistance(4.5f),
    };

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESCannibalizeTargetActionEvent>(OnCannibalizeTargetAction);
        SubscribeLocalEvent<ESCannibalizeDoAfterEvent>(OnCannibalizeDoAfter);
    }

    private void OnCannibalizeTargetAction(ESCannibalizeTargetActionEvent args)
    {
        // Can't predict due to needing to check for secret identities and whatnot
        if (_net.IsClient)
            return;

        if (args.Performer == args.Target)
            return;

        if (!_mind.TryGetMind(args.Performer, out _) ||
            !_secretIdentity.TryGetLastSecretIdentity(args.Target, out _) ||
            !HasComp<ESCryohuskableComponent> (args.Target))
        {
            _popup.PopupEntity(Loc.GetString("es-cannibal-popup-invalid"), args.Performer, args.Performer);
            return;
        }

        if (!_mobState.IsDead(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("es-cannibal-popup-not-dead"), args.Target, args.Performer);
            return;
        }

        var sound = _audio.PlayPvs(EatSound, args.Performer)?.Entity;

        args.Handled = _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.Performer,
            args.EatTime,
            new ESCannibalizeDoAfterEvent
            {
                Sound = sound,
            },
            null,
            args.Target)
        {
            Broadcast = true,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
            BreakOnMove = true,
            BreakOnDamage = true,
        });
    }

    private void OnCannibalizeDoAfter(ESCannibalizeDoAfterEvent args)
    {
        if (args.Cancelled || args.Target is not { } target)
        {
            _audio.Stop(args.Sound);
            return;
        }

        if (!_mind.TryGetMind(args.User, out var mind) ||
            !HasComp<ESCryohuskableComponent> (target))
        {
            return;
        }

        var msg = Loc.GetString("es-cannibal-stagehand-notif",
            ("player", _stagehandNotifications.WrapEntityName(args.User)),
            ("other", _stagehandNotifications.WrapEntityName(target)));
        _stagehandNotifications.SendStagehandNotification(msg);

        // This happens separately with tolerance due to this check failing
        // on the client due to the S.I. not being networked.
        if (_secretIdentity.TryGetLastSecretIdentity(target, out var secretIdentity))
        {
            _secretIdentity.ChangeSecretIdentity(mind.Value, secretIdentity.Value);
        }

        _hunger.ModifySatiety(args.User, 4); // feeling full :-)
        _cryohusk.Cryohusk(target);
    }
}

public sealed partial class ESCannibalizeTargetActionEvent : EntityTargetActionEvent
{
    [DataField]
    public TimeSpan EatTime = TimeSpan.FromSeconds(10);
}

[Serializable, NetSerializable]
public sealed partial class ESCannibalizeDoAfterEvent : SimpleDoAfterEvent
{
    [NonSerialized]
    public EntityUid? Sound;
}
