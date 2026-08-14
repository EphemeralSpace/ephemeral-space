using System.Linq;
using Content.Shared._ES.Chat;
using Content.Shared._Offbrand.Wounds;

namespace Content.Server._Offbrand.Wounds;

public sealed partial class ShockGaspThresholdsSystem : EntitySystem
{
    [Dependency] private ESEmoteSystem _emote = default!;
    [Dependency] private PainSystem _pain = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShockGaspThresholdsComponent, AfterShockChangeEvent>(OnAfterShockChange);
    }

    private void OnAfterShockChange(Entity<ShockGaspThresholdsComponent> ent, ref AfterShockChangeEvent args)
    {
        var shock = _pain.GetShock(ent.Owner);

        var message = ent.Comp.MessageThresholds.HighestMatch(shock);
        if (message == ent.Comp.CurrentMessage)
            return;

        var previousMessage = ent.Comp.CurrentMessage;

        ent.Comp.CurrentMessage = message;
        Dirty(ent);

        if (previousMessage is { } previous)
        {
            var previousKey = ent.Comp.MessageThresholds.FirstOrDefault(x => x.Value == previous).Key;
            var currentKey = ent.Comp.MessageThresholds.FirstOrDefault(x => x.Value == message).Key;

            if (previousKey >= currentKey)
            {
                return;
            }
        }

        if (message is { } msg)
            _emote.TryEmoteWithChat(ent.Owner, msg, ignoreActionBlocker: true);
    }
}
