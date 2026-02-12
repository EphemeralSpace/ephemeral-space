using Content.Server._ES.Masks.Hitman.Components;
using Content.Shared._ES.Auditions;
using Content.Shared._ES.Auditions.Components;
using Content.Shared._ES.Masks.Traitor;
using Content.Shared._ES.Objectives;
using Content.Shared._ES.Objectives.Target;
using Content.Shared._ES.Objectives.Target.Components;
using Content.Shared.Mind;
using Content.Shared.Paper;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Server._ES.Masks.Hitman;

public sealed class ESSpawnTargetDossierSystem : EntitySystem
{
    [Dependency] private readonly ESCluesSystem _clues = default!;
    [Dependency] private readonly ESSharedObjectiveSystem _objective = default!;
    [Dependency] private readonly ESTargetObjectiveSystem _target = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESSpawnTargetDossierComponent, ESCacheRevealedEvent>(OnCacheRevealed);
    }

    private void OnCacheRevealed(Entity<ESSpawnTargetDossierComponent> ent, ref ESCacheRevealedEvent args)
    {
        var objectives = _objective.GetObjectives<ESTargetCodenameComponent>(ent.Owner);
        var container = _container.GetContainer(args.Cache, ent.Comp.ContainerId);
        foreach (var obj in objectives)
        {
            if (obj.Comp.Codename is null)
                continue;

            if (!TryComp<ESTargetObjectiveComponent>(obj, out var targetComp))
                continue;

            if (!_target.TryGetTarget((obj, targetComp), out var target))
                continue;

            if (!_mind.TryGetMind(target.Value, out var targetMindId, out _))
                continue;

            var paper = SpawnClueFile(ent, targetMindId, obj.Comp.Codename);
            _container.Insert(paper, container);
        }
    }

    private EntityUid SpawnClueFile(Entity<ESSpawnTargetDossierComponent> ent, Entity<ESCharacterComponent?> mind, string codeName)
    {
        var paper = Spawn(ent.Comp.PaperPrototype, Transform(ent).Coordinates);
        var msg = GetClueMessage(ent, mind, Loc.GetString(codeName));
        _paper.SetContent(paper, msg.ToMarkup());

        return paper;
    }

    private FormattedMessage GetClueMessage(Entity<ESSpawnTargetDossierComponent> ent, Entity<ESCharacterComponent?> mind, string codeName)
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
