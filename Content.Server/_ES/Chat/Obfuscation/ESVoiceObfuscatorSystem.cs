using Content.Shared._ES.Chat.Obfuscation;
using Content.Shared._ES.Chat.Obfuscation.Components;
using Content.Shared.Chat;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;

namespace Content.Server._ES.Chat.Obfuscation;

/// <inheritdoc/>
public sealed partial class ESVoiceObfuscatorSystem : ESSharedVoiceObfuscatorSystem
{
    [Dependency] private MaskSystem _secretIdentity = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESVoiceObfuscatorComponent, InventoryRelayedEvent<TransformSpeakerNameEvent>>(OnTransformSpeakerName);
    }

    private void OnTransformSpeakerName(Entity<ESVoiceObfuscatorComponent> ent, ref InventoryRelayedEvent<TransformSpeakerNameEvent> args)
    {
        if (_secretIdentity.IsToggled(ent.Owner))
            return;

        args.Args.VoiceName = GetObfuscatedVoice(args.Owner);
    }
}
