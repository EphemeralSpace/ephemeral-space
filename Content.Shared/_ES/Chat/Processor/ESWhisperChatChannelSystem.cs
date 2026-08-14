using System.Text;
using Content.Shared._ES.Chat.Processor.Components;
using Content.Shared.Ghost;
using Robust.Shared.Random;

namespace Content.Shared._ES.Chat.Processor;

public sealed partial class ESWhisperChatChannelSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESWhisperChatChannelComponent, ESRecipientTransformChatMessageEvent>(OnRecipientTransformChatMessage);
    }

    private void OnRecipientTransformChatMessage(Entity<ESWhisperChatChannelComponent> ent, ref ESRecipientTransformChatMessageEvent args)
    {
        var xform = Transform(args.Source);
        var otherXform = Transform(args.Recipient);

        // Ghost hearing never gets obfuscated
        if (HasComp<GhostHearingComponent>(args.Recipient))
            return;

        var obfuscate = !xform.Coordinates.TryDistance(EntityManager, _transform, otherXform.Coordinates, out var distance)
                        || distance > ent.Comp.ClearHearingRange;

        if (obfuscate)
        {
            args.Content = ObfuscateMessageReadability(args.Content, ent.Comp.ObfuscateChance);
        }
    }

    private string ObfuscateMessageReadability(string message, float chance)
    {
        var modifiedMessage = new StringBuilder(message);

        for (var i = 0; i < message.Length; i++)
        {
            if (char.IsWhiteSpace((modifiedMessage[i])))
            {
                continue;
            }

            // TODO: don't use random for prediction reasons.
            if (_random.Prob(chance))
            {
                modifiedMessage[i] = '~';
            }
        }

        return modifiedMessage.ToString();
    }
}
