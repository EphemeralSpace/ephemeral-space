using Content.Server._ES.Chat;
using Content.Shared._ES.Chat;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
namespace Content.Server.Access.Systems;

public sealed partial class IdCardSystem : SharedIdCardSystem
{
    [Dependency] private ESChatSystem _chat = default!;

    public override void ExpireId(Entity<ExpireIdCardComponent> ent)
    {
        if (ent.Comp.Expired)
            return;

        base.ExpireId(ent);

        if (ent.Comp.ExpireMessage != null)
        {
            _chat.TrySendMessage(
                Loc.GetString(ent.Comp.ExpireMessage),
                ESSharedChatSystem.LocalChannel,
                ent);
        }
    }
}
