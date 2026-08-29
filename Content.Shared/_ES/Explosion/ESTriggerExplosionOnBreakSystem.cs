using Content.Shared._ES.Breakable;
using Content.Shared._ES.Explosion.Components;
using Content.Shared.Examine;
using Content.Shared.Explosion.EntitySystems;

namespace Content.Shared._ES.Explosion;

public sealed partial class ESTriggerExplosionOnBreakSystem : EntitySystem
{
    [Dependency] private SharedExplosionSystem _explosion = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESTriggerExplosionOnBreakComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ESTriggerExplosionOnBreakComponent, ESBrokenStateChanged>(OnBrokenStateChanged);
    }

    private void OnExamined(Entity<ESTriggerExplosionOnBreakComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(ESTriggerExplosionOnBreakComponent), 1))
        {
            args.PushMarkup(Loc.GetString("es-breakable-explosive-examine"));
        }
    }

    private void OnBrokenStateChanged(Entity<ESTriggerExplosionOnBreakComponent> ent, ref ESBrokenStateChanged args)
    {
        if (args.Broken)
            _explosion.TriggerExplosive(ent, delete: false, user: args.User);
    }
}
