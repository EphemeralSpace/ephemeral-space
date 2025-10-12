using Content.Shared.Administration.Managers;
using Content.Shared.Verbs;

namespace Content.Shared._ES.Masks;

public abstract class ESSharedMaskSystem : EntitySystem
{
    [Dependency] protected readonly ISharedAdminManager AdminManager = default!;

    protected static readonly VerbCategory ESMask =
        new("es-verb-categories-mask", "/Textures/Interface/emotes.svg.192dpi.png");
}
