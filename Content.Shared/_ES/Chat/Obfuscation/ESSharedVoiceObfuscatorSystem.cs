using Content.Shared._ES.Chat.Obfuscation.Components;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement.Components;

namespace Content.Shared._ES.Chat.Obfuscation;

/// <summary>
/// This handles <see cref="ESVoiceObfuscatorComponent"/>
/// </summary>
public abstract class ESSharedVoiceObfuscatorSystem : EntitySystem
{
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidAppearance = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESVoiceObfuscatorComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<ESVoiceObfuscatorComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("es-voice-obfuscator-examine"));
    }

    public string GetObfuscatedVoice(Entity<HumanoidAppearanceComponent?> ent)
    {
        // Non-humanoids have special logic since they don't have identity
        if (!Resolve(ent, ref ent.Comp, false))
        {
            if (TryComp<ESGenericVoiceComponent>(ent, out var voice))
                return Loc.GetString(voice.Voice);

            var voiceStr = Prototype(ent)?.Name ?? string.Empty;
            return Loc.GetString("es-voice-obfuscator-voice-fmt", ("voice", voiceStr));
        }

        var species = ent.Comp.Species;
        var age = ent.Comp.Age;

        var name = Name(ent);
        var gender = ent.Comp.Gender;
        var ageRepresentation = _humanoidAppearance.GetAgeRepresentation(species, age);
        var identityRepresentation = new IdentityRepresentation(name, gender, ageRepresentation);

        return Loc.GetString("es-voice-obfuscator-voice-fmt", ("voice", identityRepresentation.ToStringUnknown()));
    }
}
