using Content.Shared._ES.Degradation;
using Content.Shared._ES.Radio.Components;
using Content.Shared._ES.SecretIdentity.Traitor;
using Content.Shared._ES.Sparks;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.StatusEffectNew;
using Content.Shared.Verbs;

namespace Content.Shared._ES.Radio;

public sealed partial class ESRadioScramblerSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private ESSparksSystem _sparks = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESRadioScramblerComponent, ESUndergoDegradationEvent>(OnUndergoDegradation);
        SubscribeLocalEvent<ESRadioScramblerComponent, ESSabotageAttemptEvent>(OnSabotageAttempt);
        SubscribeLocalEvent<ESRadioScramblerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ESRadioScramblerComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerb);
        SubscribeLocalEvent<ESRadioScramblerComponent, ESRepairRadioScramblerDoAfterEvent>(OnRepairDoAfter);
    }

    private void OnUndergoDegradation(Entity<ESRadioScramblerComponent> ent, ref ESUndergoDegradationEvent args)
    {
        if (args.User is { } user)
        {
            _statusEffects.TryAddStatusEffectDuration(user,
                ent.Comp.RepairBlockStatusEffect,
                ent.Comp.RepairBlockDelay);
        }

        SetHacked(ent, true);
        args.Handled = true;
    }

    private void OnSabotageAttempt(Entity<ESRadioScramblerComponent> ent, ref ESSabotageAttemptEvent args)
    {
        if (ent.Comp.Hacked)
            args.Cancelled = true;
    }

    private void OnExamined(Entity<ESRadioScramblerComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("es-radio-scrambler-examine", ("hacked", ent.Comp.Hacked)));
    }

    private void OnGetVerb(Entity<ESRadioScramblerComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanComplexInteract || !args.CanInteract)
            return;

        var user = args.User;
        var hasSaboed = _statusEffects.HasEffectComp<ESRadioScramblerRepairBlockedStatusEffectComponent>(user);

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("es-radio-scrambler-repair-verb"),
            Disabled = !ent.Comp.Hacked || hasSaboed,
            Message = ent.Comp.Hacked && hasSaboed ? Loc.GetString("es-radio-scrambler-repair-verb-blocked") : null,
            Act = () =>
            {
                _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, ent.Comp.RepairDelay, new ESRepairRadioScramblerDoAfterEvent(), ent, ent)
                {
                    DuplicateCondition = DuplicateConditions.SameEvent,
                    BreakOnMove = true,
                    BreakOnDamage = true,
                });
            }
        });
    }

    private void OnRepairDoAfter(Entity<ESRadioScramblerComponent> ent, ref ESRepairRadioScramblerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        SetHacked(ent, false);
        _sparks.DoSparks(ent, user: args.User);
    }

    private void SetHacked(Entity<ESRadioScramblerComponent> ent, bool val)
    {
        if (ent.Comp.Hacked == val)
            return;

        ent.Comp.Hacked = val;
        Dirty(ent);

        _appearance.SetData(ent, ESRadioScramblerVisuals.Hacked, val);
    }
}
