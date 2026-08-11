using System.Diagnostics.CodeAnalysis;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Content.Shared.Players.RateLimiting;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Chat.Managers
{
    public interface IChatManager : ISharedChatManager
    {
        /// <summary>
        ///     Dispatch a server announcement to every connected player.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="colorOverride">Override the color of the message being sent.</param>
        void DispatchServerAnnouncement(string message, Color? colorOverride = null);

        void DispatchServerMessage(ICommonSession player, string message, bool suppressLog = false);

        void SendHookOOC(string sender, string message);
        void SendHookAdmin(string sender, string message);
        void SendAdminAnnouncement(string message, AdminFlags? flagBlacklist = null, AdminFlags? flagWhitelist = null);
        void SendAdminAnnouncementMessage(ICommonSession player, string message, bool suppressLog = true);

        void ChatMessageToOne(ChatChannel channel, string message, string wrappedMessage, EntityUid source, bool hideChat,
            INetChannel client, Color? colorOverride = null, bool recordReplay = false, string? audioPath = null, float audioVolume = 0, NetUserId? author = null);

        void ChatMessageToMany(ChatChannel channel, string message, string wrappedMessage, EntityUid source, bool hideChat, bool recordReplay,
            IEnumerable<INetChannel> clients, Color? colorOverride = null, string? audioPath = null, float audioVolume = 0, NetUserId? author = null);

        void ChatMessageToManyFiltered(Filter filter, ChatChannel channel, string message, string wrappedMessage, EntityUid source, bool hideChat, bool recordReplay, Color? colorOverride, string? audioPath = null, float audioVolume = 0);

        void ChatMessageToAll(ChatChannel channel, string message, string wrappedMessage, EntityUid source, bool hideChat, bool recordReplay, Color? colorOverride = null, string? audioPath = null, float audioVolume = 0, NetUserId? author = null);

        bool MessageCharacterLimit(ICommonSession player, string message);

        [return: NotNullIfNotNull(nameof(author))]
        ChatUser? EnsurePlayer(NetUserId? author);
    }
}
