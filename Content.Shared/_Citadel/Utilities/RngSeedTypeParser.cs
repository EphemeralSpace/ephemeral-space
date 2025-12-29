using Robust.Shared.Console;
using Robust.Shared.Random;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Shared._Citadel.Utilities;

public sealed class RngSeedTypeParser : TypeParser<RngSeed>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ToolshedManager _shed = default!;

    public override bool TryParse(ParserContext ctx, out RngSeed result)
    {
        result = new();
        var restore = ctx.Save();

        // If it's all hex of the right length, assume it's some existing seed.
        if (ctx.GetWord(x => x.IsAscii && char.IsAsciiHexDigit((char)x.Value)) is { Length: 32 } seed)
        {
            if (RngSeed.TryFromStringAsHex(seed, out var r))
            {
                result = r.Value;
                return true;
            }
        }

        ctx.Restore(restore);

        if (ctx.GetWord() is "random")
        {
            result = new RngSeed(_random);
            return true;
        }

        ctx.Restore(restore);

        if (_shed.TryParse(ctx, out string? str))
        {
            if (!RngSeed.TryFromStringAsSeed(str, out var r))
            {
                return false;
            }

            result = r.Value;
            return true;
        }

        return false;
    }

    public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        return CompletionResult.FromHint("`random`, a string seed, or 32-char hex seed.");
    }
}
