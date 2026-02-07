using Content.Shared._ES.Masks.Summonable.Components;
using Content.Shared._ES.Masks.Traitor;
using Content.Shared.Examine;
using Content.Shared.Mind;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared._ES.Masks.Summonable;

public sealed class ESMaskSummonSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESMaskSummonedComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ESMaskSummonerComponent, ESCacheRevealedEvent>(OnCacheRevealed);
    }

    private void OnExamined(Entity<ESMaskSummonedComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.ExamineString is not { } str)
            return;

        if (!_mind.TryGetMind(args.Examiner, out var mind, out _) ||
            mind != ent.Comp.OwnerMind)
            return;

        args.PushMarkup(Loc.GetString(str));
    }

    private void OnCacheRevealed(Entity<ESMaskSummonerComponent> ent, ref ESCacheRevealedEvent args)
    {
        foreach (var container in _container.GetAllContainers(args.Cache))
        {
            foreach (var item in container.ContainedEntities)
            {
                if (_whitelist.IsWhitelistFail(ent.Comp.Whitelist, item))
                    continue;
                var comp = EnsureComp<ESMaskSummonedComponent>(item);
                comp.OwnerMind = ent;
                comp.ExamineString = ent.Comp.ExamineString;
            }
        }
    }
}
