using System.Text;
using Content.Server._ES.Radio.Components;
using Content.Server.Radio;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._ES.Radio;

public sealed class ESRadioSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<RadioSendAttemptEvent>(OnSendAttempt);
        SubscribeLocalEvent<ESRadioFalloffComponent, RadioReceiveAttemptEvent>(OnFalloffReceiveAttempt);
    }

    private void OnSendAttempt(ref RadioSendAttemptEvent ev)
    {
        if (ev.Cancelled)
            return;

        if (_whitelist.IsWhitelistPassOrNull(ev.Channel.SenderWhitelist, ev.RadioSource))
            return;
        ev.Cancelled = true;
    }

    private void OnFalloffReceiveAttempt(Entity<ESRadioFalloffComponent> ent, ref RadioReceiveAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (_transform.InRange(args.RadioSource, args.RadioReceiver, ent.Comp.MaxSendDistance))
            return;
        args.Cancelled = true;
    }

    public string DistortMessage(Entity<ESRadioFalloffComponent?> receiver, EntityUid sender, string message)
    {
        var msg = message;

        if (!Resolve(receiver, ref receiver.Comp, false))
            return msg;

        var receiverXform = Transform(receiver);
        var senderXform = Transform(sender);

        if (!receiverXform.Coordinates.TryDistance(EntityManager, _transform, senderXform.Coordinates, out var distance))
            return msg;

        var distortion = (distance - receiver.Comp.MaxClearSendDistance) / (receiver.Comp.MaxSendDistance - receiver.Comp.MaxClearSendDistance);
        if (distortion < 0) // only distort if we're in the range.
            return msg;

        var muffleChance = MathHelper.Lerp(0.05f, 0.4f, distortion);

        var outputMsg = new StringBuilder();
        foreach (var letter in msg.AsSpan())
        {
            if (!char.IsLetterOrDigit(letter))
            {
                outputMsg.Append(letter);
                continue;
            }

            if (_random.Prob(muffleChance))
                outputMsg.Append('~');
            else
                outputMsg.Append(letter);
        }

        msg = outputMsg.ToString();
        outputMsg.Clear();

        var interjectionChance = Math.Clamp(MathHelper.Lerp(-0.1f, 0.5f, distortion), 0, 1);
        var interjection = _prototypeManager.Index(receiver.Comp.FalloffInterjectionDataset);
        foreach (var word in msg.Split(' '))
        {
            if (_random.Prob(interjectionChance))
            {
                outputMsg.Append(Loc.GetString(_random.Pick(interjection.Values)));
                outputMsg.Append(' ');
            }

            outputMsg.Append(word);
            outputMsg.Append(' ');
        }

        return outputMsg.ToString().TrimEnd();
    }
}
