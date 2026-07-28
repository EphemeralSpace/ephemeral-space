using Content.Shared._ES.Cryohusk.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Movement.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Cryohusk;

public abstract partial class ESSharedCryohuskSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private SharedIdCardSystem _idCard = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private MovementSpeedModifierSystem _movementSpeedModifier = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESCryohuskIdCardComponent, MapInitEvent>(OnCardMapInit);

        SubscribeLocalEvent<ESCryohuskComponent, MapInitEvent>(OnCryohuskMapInit);
        SubscribeLocalEvent<ESCryohuskComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
    }

    private void OnCardMapInit(Entity<ESCryohuskIdCardComponent> ent, ref MapInitEvent args)
    {
        _idCard.TryChangeFullName(ent, Loc.GetString("es-cryohusk-name"));
        _idCard.TryChangeJobTitle(ent, null);
        _idCard.TryChangeJobIcon(ent, _prototype.Index(ent.Comp.JobIcon));

        _metaData.SetEntityName(ent, Loc.GetString("es-cryohusk-id-name"));
        _metaData.SetEntityDescription(ent, Loc.GetString("es-cryohusk-id-desc"));
    }

    private void OnCryohuskMapInit(Entity<ESCryohuskComponent> ent, ref MapInitEvent args)
    {
        _movementSpeedModifier.RefreshMovementSpeedModifiers(ent);
    }

    private void OnRefreshMovementSpeedModifiers(Entity<ESCryohuskComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(ent.Comp.SpeedModifier);
    }

    public virtual void Cryohusk(Entity<ESCryohuskableComponent?> target, bool transferDeath = true)
    {
        // No op
    }
}
