using Content.Server.Chat.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Chat;
namespace Content.Server.Access.Systems;

public sealed partial class IdCardSystem : SharedIdCardSystem
{
    [Dependency] private ChatSystem _chat = default!;

    public override void ExpireId(Entity<ExpireIdCardComponent> ent)
    {
        if (ent.Comp.Expired)
            return;

        base.ExpireId(ent);

        if (ent.Comp.ExpireMessage != null)
        {
            _chat.TrySendInGameICMessage(
                ent,
                Loc.GetString(ent.Comp.ExpireMessage),
                InGameICChatType.Speak,
                ChatTransmitRange.Normal,
                true);
        }
    }
}
