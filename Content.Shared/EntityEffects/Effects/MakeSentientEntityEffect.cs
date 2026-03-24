using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class MakeSentient : EntityEffectBase<MakeSentient>
{
    /// <summary>
    /// Whether we give the target the ability to speak coherently.
    /// </summary>
    [DataField]
    public bool AllowSpeech = true;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-make-sentient", ("chance", Probability));
}
