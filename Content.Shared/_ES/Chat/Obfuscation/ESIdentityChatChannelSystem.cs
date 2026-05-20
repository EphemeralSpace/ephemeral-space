using Content.Shared.IdentityManagement;

namespace Content.Shared._ES.Chat.Obfuscation;

public sealed class ESIdentityChatChannelSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESIdentityChatChannelComponent, ESTransformMessageSourceNameEvent>(OnTransformSourceName);
    }

    private void OnTransformSourceName(Entity<ESIdentityChatChannelComponent> ent, ref ESTransformMessageSourceNameEvent args)
    {
        args.Name = Loc.GetString("es-chat-identity-name-fmt", ("name", Identity.Entity(args.Source, EntityManager)));
    }
}
