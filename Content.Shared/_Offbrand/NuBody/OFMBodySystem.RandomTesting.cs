using System.Linq;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.NuBody;

public sealed partial class OFMBodySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly MarkingManager _marking = default!;

    private List<Marking> ResolveMarkings(ProtoId<SpeciesPrototype> species, HumanoidCharacterProfile profile, HumanoidCharacterAppearance appearance)
    {
        var markingsSet = new MarkingSet();

        var speciesProto = _prototype.Index(species);
        var pointsProto = _prototype.Index(speciesProto.MarkingPoints);
        markingsSet.Points = MarkingPoints.CloneMarkingPointDictionary(pointsProto.Points);

        // General markings
        var forcedColorMarkings = new List<(Marking, MarkingPrototype)>();
        foreach (var marking in appearance.Markings)
        {
            if (!_marking.TryGetMarking(marking, out var proto))
                continue;

            // Anything with forced colouring needs to be added after everything else is situated
            if (!proto.ForcedColoring)
            {
                markingsSet.AddBack(proto.MarkingCategory, marking);
            }
            else
            {
                forcedColorMarkings.Add((marking, proto));
            }
        }

        // Hair & facial hair (I hate this)

        var facialHairColor = _marking.MustMatchSkin(species, HumanoidVisualLayers.FacialHair, out var facialHairAlpha, _prototype)
            ? appearance.SkinColor.WithAlpha(facialHairAlpha)
            : appearance.FacialHairColor;

        var facialHair = new Marking(appearance.FacialHairStyleId,
            new[] { facialHairColor });

        if (_marking.TryGetMarking(facialHair, out var facialProto) &&
            _marking.CanBeApplied(species, profile.Sex, facialProto, _prototype))
        {
            markingsSet.AddBack(facialProto.MarkingCategory, facialHair);
        }

        var hairColor = _marking.MustMatchSkin(species, HumanoidVisualLayers.Hair, out var hairAlpha, _prototype)
            ? appearance.SkinColor.WithAlpha(hairAlpha)
            : appearance.HairColor;

        var hair = new Marking(appearance.HairStyleId,
            new[] { hairColor });

        if (_marking.TryGetMarking(hair, out var hairProto) &&
            _marking.CanBeApplied(species, profile.Sex, hairProto, _prototype))
        {
            markingsSet.AddBack(hairProto.MarkingCategory, hair);
        }

        // Ensure the species of this adds up
        markingsSet.EnsureSpecies(species, appearance.SkinColor, _marking, _prototype);

        // Now we go through forced colour markings
        foreach (var (marking, prototype) in forcedColorMarkings)
        {
            var colors = MarkingColoring.GetMarkingLayerColors(
                prototype,
                appearance.SkinColor,
                appearance.EyeColor,
                markingsSet
            );

            var markingWithColor = new Marking(marking.MarkingId, colors);
            markingsSet.AddBack(prototype.MarkingCategory, markingWithColor);
        }

        // Now we ensure defaults
        markingsSet.EnsureDefault(appearance.SkinColor, appearance.EyeColor, _marking);

        return markingsSet.GetForwardEnumerator().ToList();
    }

    public void SpawnRandomNurist(ProtoId<SpeciesPrototype> species, EntityCoordinates coordinates)
    {
        var speciesProto = _prototype.Index(species);

        var profile = HumanoidCharacterProfile.RandomWithSpecies(species);
        var appearance = profile.Appearance;

        var humanoid = EntityManager.CreateEntityUninitialized(speciesProto.OFMPrototype, coordinates);
        var organContainer = _container.EnsureContainer<Container>(humanoid, OFMBodyComponent.ContainerID);

        var debug = EnsureComp<OFMDebugAppearanceComponent>(humanoid);
        debug.Appearance = appearance;
        Dirty(humanoid, debug);

        var markings = ResolveMarkings(species, profile, appearance);
        debug.Appearance.Markings = markings;

        foreach (var organProto in speciesProto.OFMOrgans)
        {
            var organ = EntityManager.CreateEntityUninitialized(organProto);
            EntityManager.InitializeAndStartEntity(organ);

            if (TryComp<OFMVisualOrganComponent>(organ, out var visualOrgan))
            {
                if (visualOrgan.Layer.Equals(HumanoidVisualLayers.Eyes))
                    visualOrgan.Data.Color = appearance.EyeColor;
                else
                    visualOrgan.Data.Color = appearance.SkinColor;

                Dirty(organ, visualOrgan);
            }

            if (TryComp<OFMVisualOrganMarkingsComponent>(organ, out var visualOrganMarkings))
            {
                foreach (var marking in markings)
                {
                    if (!_marking.TryGetMarking(marking, out var proto))
                        continue;

                    if (!visualOrganMarkings.Layers.Contains(proto.BodyPart))
                        continue;

                    if (_marking.CanBeApplied(species, profile.Sex, proto, _prototype))
                    {
                        visualOrganMarkings.Markings.Add(marking);
                    }
                }

                Dirty(organ, visualOrganMarkings);
            }

            _container.Insert(organ, organContainer);
        }

        EntityManager.InitializeAndStartEntity(humanoid);
    }
}
