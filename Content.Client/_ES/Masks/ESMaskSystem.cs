using Content.Shared._ES.Masks;
using Content.Shared.Administration;
using Content.Shared.Verbs;

namespace Content.Client._ES.Masks;

public sealed class ESMaskSystem : ESSharedMaskSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnGetVerbs(GetVerbsEvent<Verb> args)
    {
        if (AdminManager.HasAdminFlag(args.User, AdminFlags.Fun))
            args.ExtraCategories.Add(ESMask);
    }
}
