using Content.Shared._ES.Chat.Components;
using Content.Shared._ES.Chat.Processor.Components;
using Content.Shared._ES.Stagehand.Components;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Follower;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared._ES.Chat.Processor;

public sealed partial class ESStagehandFollowButtonChatChannelSystem : EntitySystem
{
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private FollowerSystem _follower = default!;

    private bool _modifyTextLinks = true;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<ChatLinkClickedRequestEvent>(OnChatMessageLinkClicked);
        SubscribeLocalEvent<ESStagehandFollowButtonChatChannelComponent, ESRecipientTransformChatMessageEvent>(OnRecipientTransformChatMessage);

        _cfg.OnValueChanged(CCVars.ChatNameLinks, val => { _modifyTextLinks = val; }, true);
    }

    private void OnRecipientTransformChatMessage(EntityUid uid, ESStagehandFollowButtonChatChannelComponent component, ref ESRecipientTransformChatMessageEvent args)
    {
        if (!_modifyTextLinks)
            return;

        // modify name to add textlink if the recipient is a stagehand
        if (!HasComp<ESStagehandComponent>(args.Recipient))
            return;

        args.Name = $"[textlink=\"(F)\" entity=\"{GetNetEntity(args.Source)}\" color=\"{Color.CornflowerBlue.ToHex()}\"] {args.Name}";
    }

    private void OnChatMessageLinkClicked(ChatLinkClickedRequestEvent msg, EntitySessionEventArgs args)
    {
        if (!_modifyTextLinks)
            return;

        if (GetEntity(msg.Target) is not { Valid: true } target || !Exists(target))
            return;

        if (args.SenderSession.AttachedEntity is not { Valid: true } ent)
            return;

        ClickMessageSender(target, ent);
    }

    /// <inheritdoc cref="CanClickMessageSender(EntityUid,EntityUid?)"/>
    public bool CanClickMessageSender(NetEntity target, EntityUid? ent = null)
    {
        return CanClickMessageSender(GetEntity(target), ent);
    }

    /// <summary>
    /// Checks whether an entity can click a chat message link.
    /// </summary>
    /// <param name="target">Target of the message link</param>
    /// <param name="ent">Entity that is attempting to click the chat message, defaults to attached player entity if null.</param>
    /// <returns>True if the entity is able to click the link</returns>
    public bool CanClickMessageSender(EntityUid target, EntityUid? ent = null)
    {
        ent ??= _player.LocalEntity;
        if (ent == null)
            return false;

        if (!CanClick(target, ent.Value))
            return false;

        return true;
    }

    private bool CanClick(EntityUid target, EntityUid ent)
    {
        if (!_modifyTextLinks)
            return false;

        if (ent == target)
            return false;

        if (!HasComp<ESStagehandComponent>(ent))
            return false;

        return true;
    }

    /// <summary>
    /// Teleports an entity to a target
    /// </summary>
    /// <param name="target">Target we are attempted to teleport to</param>
    /// <param name="ent">Entity that is attempting to warp</param>
    /// <returns>True if warp was successful.</returns>
    public void ClickMessageSender(EntityUid target, EntityUid? ent = null)
    {
        ent ??= _player.LocalEntity;
        if (ent == null)
            return;

        if (!CanClick(target, ent.Value))
            return;

        _follower.StartFollowingEntity(ent.Value, target);
    }
}
