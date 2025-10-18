using Content.Shared._ES.Masks;
using Content.Shared._ES.Masks.Components;
using Content.Shared.Administration;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Verbs;

namespace Content.Client._ES.Masks;

public sealed class ESMaskSystem : ESSharedMaskSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESTroupeFactionIconComponent, GetStatusIconsEvent>(OnGetStatusIcons);
        SubscribeLocalEvent<GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnGetStatusIcons(Entity<ESTroupeFactionIconComponent> ent, ref GetStatusIconsEvent args)
    {
        args.StatusIcons.Add(PrototypeManager.Index(ent.Comp.Icon));
    }

    private void OnGetVerbs(GetVerbsEvent<Verb> args)
    {
        if (AdminManager.HasAdminFlag(args.User, AdminFlags.Fun))
            args.ExtraCategories.Add(ESMask);
    }
}
