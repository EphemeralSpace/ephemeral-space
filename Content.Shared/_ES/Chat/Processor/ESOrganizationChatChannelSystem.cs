using Content.Shared._ES.Chat.Processor.Components;
using Content.Shared._ES.SecretIdentity;
using Content.Shared.Mind;

namespace Content.Shared._ES.Chat.Processor;

public sealed partial class ESOrganizationChatChannelSystem : EntitySystem
{
    [Dependency] private ESSharedSecretIdentitySystem _secretIdentity = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESOrganizationChatChannelComponent, ESGetChatMessageRecipientsEvent>(OnGetChatMessageRecipients);
    }

    private void OnGetChatMessageRecipients(Entity<ESOrganizationChatChannelComponent> ent, ref ESGetChatMessageRecipientsEvent args)
    {
        foreach (var mind in _secretIdentity.GetOrganizationMembers(ent.Comp.Organization))
        {
            if (!TryComp<MindComponent>(mind, out var mindComponent))
                continue;

            if (mindComponent.CurrentEntity is not { } currentEntity)
                continue;

            if (_secretIdentity.TryGetSecretIdentity((mind, mindComponent), out var secretIdentity) &&
                ent.Comp.IgnoredIdentities.Contains(secretIdentity.Value))
                continue;

            args.AddRecipient(currentEntity);
        }
    }
}
