using Content.Server.Players.RateLimiting;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Players.RateLimiting;
using Robust.Shared.Player;

namespace Content.Server._ES.Chat;

public sealed partial class ESChatManager
{
    [Dependency] private PlayerRateLimitManager _rateLimit = default!;

    private const string RateLimitKey = "Chat";

    private void RegisterRateLimits()
    {
        _rateLimit.Register(RateLimitKey,
            new RateLimitRegistration(CCVars.ChatRateLimitPeriod,
                CCVars.ChatRateLimitCount,
                RateLimitPlayerLimited,
                CCVars.ChatRateLimitAnnounceAdminsDelay,
                RateLimitAlertAdmins,
                LogType.ChatRateLimited)
        );
    }

    private void RateLimitPlayerLimited(ICommonSession player)
    {
        SendServerMessage(Loc.GetString("chat-manager-rate-limited"), player);
    }

    private void RateLimitAlertAdmins(ICommonSession player)
    {
        SendAdminMessage(Loc.GetString("chat-manager-rate-limit-admin-announcement", ("player", player.Name)));
    }

    public RateLimitStatus HandleRateLimit(ICommonSession player)
    {
        return _rateLimit.CountAction(player, RateLimitKey);
    }
}
