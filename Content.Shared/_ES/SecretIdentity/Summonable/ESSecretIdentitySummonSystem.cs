using Content.Shared._ES.SecretIdentity.Summonable.Components;
using Content.Shared._ES.SecretIdentity.Traitor;
using Content.Shared.Examine;
using Content.Shared.Mind;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared._ES.SecretIdentity.Summonable;

public sealed partial class ESSecretIdentitySummonSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESSecretIdentitySummonedComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ESSecretIdentitySummonerComponent, ESCacheRevealedEvent>(OnCacheRevealed);
    }

    private void OnExamined(Entity<ESSecretIdentitySummonedComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.ExamineString is not { } str)
            return;

        if (!_mind.TryGetMind(args.Examiner, out var mind, out _) ||
            mind != ent.Comp.OwnerMind)
            return;

        args.PushMarkup(Loc.GetString(str));
    }

    private void OnCacheRevealed(Entity<ESSecretIdentitySummonerComponent> ent, ref ESCacheRevealedEvent args)
    {
        foreach (var container in _container.GetAllContainers(args.Cache))
        {
            foreach (var item in container.ContainedEntities)
            {
                if (_whitelist.IsWhitelistFail(ent.Comp.Whitelist, item))
                    continue;
                var comp = EnsureComp<ESSecretIdentitySummonedComponent>(item);
                comp.OwnerMind = ent;
                comp.ExamineString = ent.Comp.ExamineString;
            }
        }
    }
}
