using Content.Server.Polymorph.Systems;
using Content.Shared.Changeling;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Server._ES.Changeling;

public sealed class ESChangelingDevourSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateComponent, ESEntityDevouredEvent>(OnDevoured);
    }

    public void OnDevoured(EntityUid uid, MobStateComponent component, ref ESEntityDevouredEvent args)
    {
        if (_mobState.IsDead(uid))
        {
            _polymorph.PolymorphEntity(uid, args.HuskProto);
        }
    }

}
