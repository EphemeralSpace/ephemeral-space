using System.Linq;
using Content.Server.GameTicking;
using Content.Shared.GameTicking.Components;

namespace Content.Server._ES.Masks.Masquerades;

public sealed partial class ESMasqueradeSystem
{
    [Dependency] private readonly ILocalizationManager _loc = default!;

    protected override void AppendRoundEndText(EntityUid uid, ESMasqueradeRuleComponent component, GameRuleComponent gameRule, ref RoundEndTextAppendEvent ev)
    {
        if (component.Masquerade is not { } masquerade)
            return;

        ev.AddLine(
            Loc.GetString(
                "es-roundend-masquerade-reveal",
                ("masquerade", masquerade.LocName(_loc)),
                ("description", masquerade.LocDescription(_loc))
                )
            );

        // I just want like, a couple blanks.
        ev.AddLine(string.Empty);
        ev.AddLine(string.Empty);
    }
}
