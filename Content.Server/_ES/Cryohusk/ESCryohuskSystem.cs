using Content.Server.Administration;
using Content.Server.Polymorph.Systems;
using Content.Shared.Administration;
using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server._ES.Cryohusk;

public sealed partial class ESCryohuskSystem : EntitySystem
{
    [Dependency] private PolymorphSystem _polymorph = default!;

    private static readonly ProtoId<PolymorphPrototype> CryohuskPolymorph = "ESCryohuskPolymorph";

    public void Cryohusk(EntityUid target)
    {
        _polymorph.PolymorphEntity(target, CryohuskPolymorph);
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
