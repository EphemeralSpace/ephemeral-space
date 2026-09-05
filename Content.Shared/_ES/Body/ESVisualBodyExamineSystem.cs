using System.Linq;
using Content.Shared._ES.Auditions;
using Content.Shared.Body;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.IdentityManagement.Components;
using Robust.Shared.ColorNaming;
using Robust.Shared.Prototypes;

public sealed partial class ESVisualBodyExamineSystem : EntitySystem
{
    [Dependency] private SharedVisualBodySystem _visualBody = default!;
    [Dependency] private IdentitySystem _identity = default!;
    [Dependency] private ESCluesSystem _clues = default!;

    private static readonly ProtoId<OrganCategoryPrototype> Head = "Head";
    private static readonly ProtoId<OrganCategoryPrototype> Eyes = "Eyes";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VisualBodyComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<VisualBodyComponent> ent, ref ExaminedEvent args)
    {
        if (!_visualBody.TryGatherMarkingsData(ent.AsNullable(),
                null,
                out var profiles,
                out var markings,
                out var applied))
            return;

        if (!TryComp<HumanoidProfileComponent>(ent, out var profile))
            return;

        if (!TryComp<HideableHumanoidLayersComponent>(ent, out var hideable))
            return;
        var hiddenLayers = hideable.HiddenLayers;

        var identity = Identity.Entity(ent, EntityManager);
        using var _ = args.PushGroup(nameof(VisualBodyComponent));

        var skinColor = profiles.First().Value.SkinColor;
        var skinString = _clues.GetSkinColorString(skinColor, profile.Species);
        args.PushMarkup(Loc.GetString("es-appearance-examine-skin",
            ("user", identity),
            ("color", skinColor),
            ("colorStr", skinString)));

        string hairString;
        if (!hiddenLayers.ContainsKey(HumanoidVisualLayers.Hair))
        {
            if (!applied.TryGetValue(Head, out var appliedHeadMarkings) ||
                !appliedHeadMarkings.TryGetValue(HumanoidVisualLayers.Hair, out var hairMarkings) ||
                hairMarkings.Count == 0)
            {
                hairString = Loc.GetString("es-appearance-examine-hair-bald",
                    ("user", identity));
            }
            else
            {
                var color = hairMarkings.First().MarkingColors.First();
                var hairColor = _clues.GetHairColorString(color);

                hairString = Loc.GetString("es-appearance-examine-hair",
                    ("user", identity),
                    ("color", color),
                    ("colorStr", hairColor));
            }
        }
        else
        {
            hairString = Loc.GetString("es-appearance-examine-hair-hidden",
                ("user", identity));
        }

        string eyeString;
        if (!_identity.HasIdentityBlockerCoverage(ent, IdentityBlockerCoverage.EYES))
        {
            if (profiles.TryGetValue(Eyes, out var eyes))
            {
                var eyeColor = ColorNaming.Describe(eyes.EyeColor, Loc);
                eyeString = Loc.GetString("es-appearance-examine-eyes",
                    ("user", identity),
                    ("colorStr", eyeColor),
                    ("color", eyes.EyeColor));
            }
            else
            {
                eyeString = Loc.GetString("es-appearance-examine-eyes-none",
                    ("user", identity));
            }
        }
        else
        {
            eyeString = Loc.GetString("es-appearance-examine-eyes-hidden",
                ("user", identity));
        }

        args.PushMarkup(Loc.GetString("es-appearance-examine-splice", ("hair", hairString), ("eye", eyeString)));

    }
}
