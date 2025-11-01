using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.Markers;

public sealed class MarkerSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private bool _markersVisible;

    public bool MarkersVisible
    {
        get => _markersVisible;
        set
        {
            _markersVisible = value;
            UpdateMarkers();
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MarkerComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, MarkerComponent marker, ComponentStartup args)
    {
        UpdateVisibility((uid, marker));
    }

    // ES START
    // if layers are set, only toggle layers instead of the entire sprite
    private void UpdateVisibility(Entity<MarkerComponent> uid)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;


        if (uid.Comp.Layers is null)
        {
            _sprite.SetVisible((uid, sprite), MarkersVisible);
        }
        else
        {
            foreach (var layer in uid.Comp.Layers)
            {
                _sprite.LayerSetVisible((uid, sprite), layer, MarkersVisible);
            }
        }
    }
    // ES END

    private void UpdateMarkers()
    {
        var query = AllEntityQuery<MarkerComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            // ES START
            UpdateVisibility((uid, comp));
            // ES END
        }
    }
}
