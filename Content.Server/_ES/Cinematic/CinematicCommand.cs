using Content.Server.Administration;
using Content.Shared._ES.Cinematic;
using Content.Shared.Administration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server._ES.Cinematic;

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class CinematicCommand : ToolshedCommand
{
    private ESCinematicSystem? _cinematic;

    [CommandImplementation("playAll")]
    public void PlayAll([CommandArgument] ProtoId<ESCinematicPrototype> cinematic)
    {
        _cinematic ??= GetSys<ESCinematicSystem>();
        _cinematic.PlayCinematic(cinematic, Filter.Broadcast());
    }
}
