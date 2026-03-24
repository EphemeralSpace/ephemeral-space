using Content.Server.Administration.Managers;
using Content.Shared._Offbrand.Wounds;
using Content.Shared.Administration;
using Content.Shared.Verbs;

namespace Content.Server._ES.Debugging;

/// <summary>
///     Various verbs we use to debug specific things
/// </summary>
public sealed class ESDebugVerbsSystem : EntitySystem
{
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly BrainDamageSystem _brain = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnGetVerbs(GetVerbsEvent<Verb> args)
    {
        if (!_admin.HasAdminFlag(args.User, AdminFlags.Debug))
            return;

        Verb verb = new()
        {
            Text = Loc.GetString("es-verb-kill"),
            Category = VerbCategory.Debug,
            Act = () => _brain.KillBrain(args.Target),
        };
        args.Verbs.Add(verb);
    }
}
