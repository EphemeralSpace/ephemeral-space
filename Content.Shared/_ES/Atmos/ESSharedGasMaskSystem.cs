using System.Linq;
using Content.Shared._ES.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.Localizations;

namespace Content.Shared._ES.Atmos;

public abstract partial class ESSharedGasMaskSystem : EntitySystem
{
    [Dependency] private SharedAtmosphereSystem _atmosphere = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESGasMaskComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<ESGasMaskComponent> ent, ref ExaminedEvent args)
    {
        var gases = ent.Comp.BlockedGases
            .Select(gas => Loc.GetString("es-gas-mask-gas-fmt",
                ("name", Loc.GetString(_atmosphere.GetGas(gas).Name))))
            .ToList();

        args.PushMarkup(Loc.GetString("es-gas-mask-gas-examine",
            ("gas", ContentLocalizationManager.FormatList(gases))));
    }
}
