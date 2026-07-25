using Content.Shared._ES.Cryohusk.Components;
using Content.Shared.Access.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Cryohusk;

public abstract partial class ESSharedCryohuskSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private SharedIdCardSystem _idCard = default!;
    [Dependency] private MetaDataSystem _metaData = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESCryohuskIdCardComponent, MapInitEvent>(OnCardMapInit);
    }

    private void OnCardMapInit(Entity<ESCryohuskIdCardComponent> ent, ref MapInitEvent args)
    {
        _idCard.TryChangeFullName(ent, "???");
        _idCard.TryChangeJobTitle(ent, null);
        _idCard.TryChangeJobIcon(ent, _prototype.Index(ent.Comp.JobIcon));

        _metaData.SetEntityName(ent, Loc.GetString("es-cryohusk-id-name"));
        _metaData.SetEntityDescription(ent, Loc.GetString("es-cryohusk-id-desc"));
    }

    public virtual void Cryohusk(Entity<ESCryohuskableComponent?> target)
    {
        // No op
    }
}
