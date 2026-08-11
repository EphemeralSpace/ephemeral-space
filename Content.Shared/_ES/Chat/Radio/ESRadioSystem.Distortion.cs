using System.Text;
using Content.Shared._ES.Radio.Components;
using Content.Shared.Dataset;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._ES.Chat.Radio;

public sealed partial class ESRadioSystem
{
    private static readonly ProtoId<LocalizedDatasetPrototype> FalloffInterjectionDataset = "ESRadioFalloffInterjections";

    public bool IsGlobalDistortActive()
    {
        var query = EntityQueryEnumerator<ESRadioScramblerComponent>();
        while (query.MoveNext(out var comp))
        {
            if (comp.Hacked)
                return true;
        }

        return false;
    }

    public static string DistortRadioMessage(string msg, float a, IPrototypeManager protoMan, IRobustRandom random, ILocalizationManager loc)
    {
        var muffleChance = MathHelper.Lerp(0.05f, 0.4f, a);

        var outputMsg = new StringBuilder();
        foreach (var letter in msg.AsSpan())
        {
            if (!char.IsLetterOrDigit(letter))
            {
                outputMsg.Append(letter);
                continue;
            }

            if (random.Prob(muffleChance))
                outputMsg.Append('~');
            else
                outputMsg.Append(letter);
        }

        msg = outputMsg.ToString();
        outputMsg.Clear();

        var interjectionChance = Math.Clamp(MathHelper.Lerp(-0.1f, 0.5f, a), 0, 1);
        var interjection = protoMan.Index(FalloffInterjectionDataset);
        foreach (var word in msg.Split(' '))
        {
            if (random.Prob(interjectionChance))
            {
                outputMsg.Append(loc.GetString(random.Pick(interjection.Values)));
                outputMsg.Append(' ');
            }

            outputMsg.Append(word);
            outputMsg.Append(' ');
        }

        return outputMsg.ToString().TrimEnd();
    }
}
