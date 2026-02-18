using Content.Shared._ES.Degradation;
using Content.Shared._ES.Radio.Components;

namespace Content.Shared._ES.Radio;

public sealed class ESSharedRadioSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESRadioScramblerComponent, ESUndergoDegradationEvent>(OnUndergoDegradation);
    }

    private void OnUndergoDegradation(Entity<ESRadioScramblerComponent> ent, ref ESUndergoDegradationEvent args)
    {
        ent.Comp.Hacked = true;
        Dirty(ent);

        args.Handled = true;
    }
}
