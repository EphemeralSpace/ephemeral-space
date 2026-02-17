using System.Linq;
using Content.Shared._ES.Auditions.Components;
using Content.Shared._ES.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._ES.Auditions;

/// <summary>
/// The main system for handling the creation, integration of relations
/// </summary>
public abstract partial class ESSharedAuditionsSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public bool RandomCharactersEnabled;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_config, ESCVars.ESRandomCharacters, val => RandomCharactersEnabled = val, true);

        SubscribeLocalEvent<ESProducerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ESProducerComponent> ent, ref MapInitEvent args)
    {
        var dataset = new List<LocId>(_prototypeManager.Index(ent.Comp.OpinionDataset).Values.Select(n => (LocId) n));
        ent.Comp.OpinionConcepts.AddRange(_random.GetItems(dataset, ent.Comp.OpinionConceptCount, allowDuplicates: false));
    }

    /// <summary>
    /// Returns the number of characters on the station.
    /// Only counts the number of people that have been spawned across the round,
    /// does not account for people leaving or disconnecting.
    /// </summary>
    public int GetPlayerCount()
    {
        var count = 0;
        var query = EntityQueryEnumerator<ESProducerComponent>();
        while (query.MoveNext(out var comp))
        {
            count += comp.Characters.Count - comp.UnusedCharacterPool.Count;
        }

        return count;
    }

    public IEnumerable<Entity<ESCharacterComponent>> GetCharacters()
    {
        var query = EntityQueryEnumerator<ESProducerComponent>();
        while (query.MoveNext(out var comp))
        {
            foreach (var character in comp.UsedCharacters)
            {
                if (TryComp<ESCharacterComponent>(character, out var c))
                    yield return (character, c);
            }
        }
    }

    public string GetCharacterPrompt(Entity<ESCharacterComponent> ent)
    {
        if (!_job.MindTryGetJobId(ent, out var jobId))
            jobId = SharedGameTicker.FallbackOverflowJob;

        var job = _prototypeManager.Index(jobId);
        return Loc.GetString("es-character-personality-prompt",
            ("descriptor", ent.Comp.Descriptor),
            ("job", job.LocalizedName.ToLowerInvariant()),
            ("focus", ent.Comp.Focus));
    }
}
