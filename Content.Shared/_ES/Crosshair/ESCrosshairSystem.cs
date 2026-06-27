namespace Content.Shared._ES.Crosshair;

public sealed class ESCrosshairSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ESCrosshairNetworkEvent>(OnCrosshair);
    }

    private void OnCrosshair(ESCrosshairNetworkEvent msg, EntitySessionEventArgs args)
    {
        if (msg.User is not { } user)
            return;

        var ent = GetEntity(user);
        if (args.SenderSession.AttachedEntity != ent
            || !TryComp<ESCrosshairAimerComponent>(ent, out var aimer))
            return;

        var crosshair = aimer.CrosshairEntity;
        if (crosshair is null) // wait for it to get spawned
            return;

        // todo distance check prolly
        var userXform = Transform(ent);
        if (userXform.MapID != msg.Coordinates.MapId)
            return;

        _xform.SetMapCoordinates(crosshair.Value, msg.Coordinates);
    }
}
