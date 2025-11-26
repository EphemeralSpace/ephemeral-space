using Content.Server._ES.Chat.Obfuscation.Components;
using Content.Server.Humanoid;
using Content.Shared.Chat;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Inventory;

namespace Content.Server._ES.Chat.Obfuscation;

/// <summary>
/// This handles <see cref="ESVoiceObfuscatorComponent"/>
/// </summary>
public sealed class ESVoiceObfuscatorSystem : EntitySystem
{
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly MaskSystem _mask = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESVoiceObfuscatorComponent, InventoryRelayedEvent<TransformSpeakerNameEvent>>(OnTransformSpeakerName);
    }

    private void OnTransformSpeakerName(Entity<ESVoiceObfuscatorComponent> ent, ref InventoryRelayedEvent<TransformSpeakerNameEvent> args)
    {
        if (_mask.IsToggled(ent.Owner))
            return;

        args.Args.VoiceName = GetObfuscatedVoice(args.Owner);
    }

    private string GetObfuscatedVoice(Entity<HumanoidAppearanceComponent?> ent)
    {
        // They need to have this component.
        if (!Resolve(ent, ref ent.Comp))
            return string.Empty;

        var species = ent.Comp.Species;
        var age = ent.Comp.Age;

        var name = Name(ent);
        var gender = ent.Comp.Gender;
        var ageRepresentation = _humanoidAppearance.GetAgeRepresentation(species, age);
        var identityRepresentation = new IdentityRepresentation(name, gender, ageRepresentation);

        return identityRepresentation.ToStringUnknown();
    }
}
