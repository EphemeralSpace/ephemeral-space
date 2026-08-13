using Content.Client.UserInterface.Systems.Chat;
using Content.Shared._ES.Mapping;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;

namespace Content.Client._ES.Mapping;

public sealed partial class ESMapperSelectionOverlays : Overlay
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;

    private readonly ChatUIController _chat;
    private readonly ESSelectionSystem _selection;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public ESMapperSelectionOverlays()
    {
        IoCManager.InjectDependencies(this);
        _chat = _ui.GetUIController<ChatUIController>();
        _selection = _entManager.System<ESSelectionSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;

        var query = _entManager.AllEntityQueryEnumerator<ESMapperComponent>();
        while (query.MoveNext(out var uid, out var mapper))
        {
            if (mapper.SelectionState is not ESSelectionState.Selecting { Selection: var selection })
                continue;

            var box = _selection.ToWorldBox2Rotated(selection);
            if (box.Item1 != args.MapId)
                continue;

            var name = _entManager.GetComponent<MetaDataComponent>(uid).EntityName;
            var color = _chat.GetNameColor(name);
            var (x, y) = box.Item2.Box.Size;
            if (x <= 0.1f || y <= 0.1f)
                continue;

            handle.DrawRect(box.Item2, color.WithAlpha(0.1f));
            handle.DrawRect(box.Item2, color, false);
        }
    }
}
