using System.Linq;
using Content.Server._ES.SecretIdentity.Insider.Components;
using Content.Shared._ES.Auditions;
using Content.Shared._ES.Auditions.Components;
using Content.Shared._ES.SecretIdentity;
using Content.Shared.Paper;
using Robust.Server.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._ES.SecretIdentity.Insider;

public sealed partial class ESTroupeDossierSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ESCluesSystem _clues = default!;
    [Dependency] private ContainerSystem _container = default!;
    [Dependency] private ESSecretIdentitySystem _secretIdentity = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private PaperSystem _paper = default!;

    private static readonly ProtoId<ESTroupePrototype> CrewTroupe = "Crew";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESTroupeDossierComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ESTroupeDossierComponent> ent, ref MapInitEvent args)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            return;

        var papers = new List<EntityUid>();
        var codenames = new List<string>(_prototype.Index(ent.Comp.CodenameDataset).Values);

        var crewMinds = _secretIdentity.GetTroupeMembers(CrewTroupe).ToList();
        for (var i = 0; i < Math.Min(ent.Comp.CrewCount, crewMinds.Count); i++)
        {
            var mind = _random.PickAndTake(crewMinds);
            papers.Add(SpawnClueFile(ent, mind, Loc.GetString(_random.PickAndTake(codenames))));
        }

        var notCrewMinds = _secretIdentity.GetNotTroupeMembers(CrewTroupe).ToList();
        for (var i = 0; i < Math.Min(ent.Comp.NonCrewCount, notCrewMinds.Count); i++)
        {
            var mind = _random.PickAndTake(notCrewMinds);
            papers.Add(SpawnClueFile(ent, mind, Loc.GetString(_random.PickAndTake(codenames))));
        }

        var briefing = SpawnBriefing(ent);
        _container.Insert(briefing, container);

        _random.Shuffle(papers);
        foreach (var paper in papers)
        {
            _container.Insert(paper, container);
        }
    }

    private EntityUid SpawnBriefing(Entity<ESTroupeDossierComponent> ent)
    {
        var paper = Spawn(ent.Comp.PaperPrototype);
        var text = Loc.GetString("es-troupe-dossier-briefing-text",
            ("sum", ent.Comp.CrewCount + ent.Comp.NonCrewCount),
            ("crew", ent.Comp.CrewCount),
            ("noncrew", ent.Comp.NonCrewCount));
        _paper.SetContent(paper, text);
        _metaData.SetEntityName(paper, Loc.GetString("es-troupe-dossier-briefing-name"));
        return paper;
    }

    private EntityUid SpawnClueFile(Entity<ESTroupeDossierComponent> ent, Entity<ESCharacterComponent?> mind, string codeName)
    {
        var paper = Spawn(ent.Comp.PaperPrototype);
        _paper.SetContent(paper, GetClueMessage(ent, mind, codeName).ToMarkup());
        _metaData.SetEntityName(paper, Loc.GetString("es-troupe-dossier-name", ("name", codeName)));
        return paper;
    }

    private FormattedMessage GetClueMessage(Entity<ESTroupeDossierComponent> ent, Entity<ESCharacterComponent?> mind, string codeName)
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
        return msg;
    }
}
