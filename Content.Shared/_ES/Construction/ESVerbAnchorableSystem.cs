using Content.Shared._ES.Construction.Components;
using Content.Shared.Construction.Components;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared._ES.Construction;

public sealed partial class ESVerbAnchorableSystem : EntitySystem
{
    [Dependency] private AnchorableSystem _anchorable = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private EntityQuery<AnchorableComponent> _anchorableQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESVerbAnchorableComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<ESVerbAnchorableComponent, ESToggleAnchorDoAfterEvent>(OnToggleAnchorDoAfter);
    }

    private void OnGetVerbs(Entity<ESVerbAnchorableComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess)
            return;

        if (!_anchorableQuery.TryComp(ent, out var anchorable))
        {
            Log.Error($"Entity {ToPrettyString(ent)} with {nameof(ESVerbAnchorableComponent)} doesn't have component {nameof(AnchorableComponent)}!");
            return;
        }

        var user = args.User;
        var canUse = args.CanInteract && args.CanComplexInteract;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Transform(ent).Anchored
                ? Loc.GetString("es-verb-anchorable-title-unanchor")
                : Loc.GetString("es-verb-anchorable-title-anchor"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/anchor.svg.192dpi.png")),
            Disabled = !canUse,
            Act = () =>
            {
                _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
                    user,
                    anchorable.Delay,
                    new ESToggleAnchorDoAfterEvent(),
                    ent,
                    target: ent,
                    used: user)
                {
                    BreakOnDamage = true,
                    BreakOnMove = true,
                    BreakOnWeightlessMove = false,
                    DuplicateCondition = DuplicateConditions.SameEvent,
                });
            },
        });
    }

    private void OnToggleAnchorDoAfter(Entity<ESVerbAnchorableComponent> ent, ref ESToggleAnchorDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        // TODO: anchor.
    }
}
