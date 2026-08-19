using Content.Shared.CombatMode;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed partial class DisruptOnAttackStatusEffectSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisruptOnAttackEvent>(OnDisruptOnAttack);
        SubscribeLocalEvent<DisruptOnAttackStatusEffectComponent, StatusEffectRelayedEvent<DamageChangedEvent>>(OnDamageChanged);
    }

    private void OnDisruptOnAttack(DisruptOnAttackEvent args)
    {
        var disarm = new DisarmedEvent(args.Damaged, args.Origin, 1f);
        RaiseLocalEvent(args.Damaged, ref disarm);

        if (!_statusEffects.HasEffectComp<ESPainStunCooldownStatusEffectComponent>(args.Damaged))
        {
            _statusEffects.TryAddStatusEffectDuration(args.Damaged, args.StunStatusEffect, args.StunDuration);
            _statusEffects.TryAddStatusEffectDuration(args.Damaged, args.CooldownEffect, args.StunDuration + args.Cooldown);
        }

        _userInterface.CloseUserUis(args.Damaged);
    }

    private void OnDamageChanged(Entity<DisruptOnAttackStatusEffectComponent> ent, ref StatusEffectRelayedEvent<DamageChangedEvent> args)
    {
        if (!args.Args.DamageIncreased)
            return;

        if (args.Args.Origin is not {} origin)
            return;

        if (Comp<StatusEffectComponent>(ent).AppliedTo is not { } damaged)
            return;

        if (!HasComp<MobStateComponent>(origin))
            return;

        QueueLocalEvent(new DisruptOnAttackEvent(damaged, origin, ent.Comp.PainStunStatusEffect, ent.Comp.CooldownStatusEffect, ent.Comp.PainStunDuration, ent.Comp.CooldownDuration));
    }
}

public sealed class DisruptOnAttackEvent(EntityUid damaged, EntityUid origin, EntProtoId stunStatusEffect, EntProtoId cooldownEffect, TimeSpan stunDuration, TimeSpan cooldown) : EntityEventArgs
{
    public readonly EntityUid Damaged = damaged;
    public readonly EntityUid Origin = origin;
    public readonly EntProtoId StunStatusEffect = stunStatusEffect;
    public readonly EntProtoId CooldownEffect = cooldownEffect;
    public readonly TimeSpan StunDuration = stunDuration;
    public readonly TimeSpan Cooldown = cooldown;

}

