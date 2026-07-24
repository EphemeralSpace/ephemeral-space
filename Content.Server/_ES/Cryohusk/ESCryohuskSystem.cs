using Content.Server._ES.Cryohusk.Components;
using Content.Server.Administration;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Polymorph.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Robust.Server.Audio;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Toolshed;

namespace Content.Server._ES.Cryohusk;

public sealed partial class ESCryohuskSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private PolymorphSystem _polymorph = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESCryohuskableComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ESCryohuskableComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime;
    }

    public void Cryohusk(Entity<ESCryohuskableComponent?> target)
    {
        if (!Resolve(target, ref target.Comp))
            return;

        if (_polymorph.PolymorphEntity(target, target.Comp.CryohuskPolymorph) is not { } husk)
            return;

        _audio.PlayPvs(target.Comp.FreezeSound, husk);
    }

    public override void Update(float frameTime)
    {
        foreach (var (uid, comp, xform) in EntityQueryEnumerator<ESCryohuskableComponent, TransformComponent>())
        {
            if (_timing.CurTime < comp.NextUpdate)
                continue;
            comp.NextUpdate += comp.UpdateRate;

            // Must be unconscious
            if (_actionBlocker.CanConsciouslyPerformAction(uid))
                continue;

            if (_atmosphere.GetTileMixture((uid, xform)) is not { } mix ||
                mix.GetMoles(Gas.Cryogas) < comp.MinConversionMols)
                continue;

            if (!_random.Prob(comp.ConversionChance))
                continue;

            Cryohusk(uid);
        }
    }
}

[ToolshedCommand, AdminCommand(AdminFlags.Fun)]
public sealed partial class ESCryohuskCommand : ToolshedCommand
{
    [Dependency] private IEntityManager _entityManager = default!;
    private ESCryohuskSystem? _cryohusk;

    [CommandImplementation("cryohusk")]
    public void Cryohusk([PipedArgument] EntityUid target)
    {
        _cryohusk ??= _entityManager.System<ESCryohuskSystem>();
        _cryohusk.Cryohusk(target);
    }
}
