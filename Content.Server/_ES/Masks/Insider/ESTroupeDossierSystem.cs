using System.Linq;
using Content.Server._ES.Masks.Insider.Components;
using Content.Shared._ES.Auditions;
using Content.Shared._ES.Auditions.Components;
using Content.Shared._ES.Masks;
using Content.Shared.Paper;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._ES.Masks.Insider;

public sealed class ESTroupeDossierSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ESCluesSystem _clues = default!;
    [Dependency] private readonly ESMaskSystem _mask = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly PaperSystem _paper = default!;

    private static readonly ProtoId<ESTroupePrototype> CrewTroupe = "ESCrew";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESTroupeDossierComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ESTroupeDossierComponent> ent, ref MapInitEvent args)
    {
        var codenames = new List<string>(_prototype.Index(ent.Comp.CodenameDataset).Values);

        var crewMinds = _mask.GetTroupeMembers(CrewTroupe).ToList();
        for (var i = 0; i < Math.Min(ent.Comp.CrewCount, crewMinds.Count); i++)
        {
            var mind = _random.PickAndTake(crewMinds);
            SpawnClueFile(ent, mind, Loc.GetString(_random.PickAndTake(codenames)), true);
        }

        var notCrewMinds = _mask.GetNotTroupeMembers(CrewTroupe).ToList();
        for (var i = 0; i < Math.Min(ent.Comp.NonCrewCount, notCrewMinds.Count); i++)
        {
            var mind = _random.PickAndTake(notCrewMinds);
            SpawnClueFile(ent, mind, Loc.GetString(_random.PickAndTake(codenames)), false);
        }
    }

    private void SpawnClueFile(Entity<ESTroupeDossierComponent> ent, Entity<ESCharacterComponent?> mind, string codeName, bool isCrew)
    {
        var paper = SpawnInContainerOrDrop(ent.Comp.PaperPrototype, ent, ent.Comp.ContainerId);
        var msg = GetClueMessage(ent, mind, codeName, isCrew);
        _paper.SetContent(paper, msg.ToMarkup());
        _metaData.SetEntityName(paper, Loc.GetString("es-troupe-dossier-name", ("name", codeName)));
    }

    private FormattedMessage GetClueMessage(Entity<ESTroupeDossierComponent> ent, Entity<ESCharacterComponent?> mind, string codeName, bool isCrew)
    {
        var msg = new FormattedMessage();

        if (!Resolve(mind, ref mind.Comp))
            return msg;

        msg.AddMarkupOrThrow(Loc.GetString("es-troupe-dossier-header", ("name", codeName)));
        msg.PushNewline();

        foreach (var clue in _clues.GetClues(mind, ent.Comp.ClueCount))
        {
            msg.AddMarkupOrThrow(Loc.GetString("es-troupe-dossier-clue-fmt", ("clue", clue)));
            msg.PushNewline();
        }

        msg.AddMarkupOrThrow(isCrew
            ? Loc.GetString("es-troupe-dossier-footer-crew")
            : Loc.GetString("es-troupe-dossier-footer-not-crew"));
        return msg;
    }
}
