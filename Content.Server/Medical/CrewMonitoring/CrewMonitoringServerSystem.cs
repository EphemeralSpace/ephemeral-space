using Content.Server.DeviceNetwork.Systems;
using Content.Server.Medical.SuitSensors;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Timing;
using Content.Shared.DeviceNetwork.Components;
// ES START
using Content.Shared._ES.Degradation;
using Content.Shared.Medical.SuitSensors;
// ES END

namespace Content.Server.Medical.CrewMonitoring;

public sealed class CrewMonitoringServerSystem : EntitySystem
{
    [Dependency] private readonly SuitSensorSystem _sensors = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly SingletonDeviceNetServerSystem _singletonServerSystem = default!;
// ES START
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
// ES END

    private const float UpdateRate = 3f;
    private float _updateDiff;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrewMonitoringServerComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<CrewMonitoringServerComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<CrewMonitoringServerComponent, DeviceNetServerDisconnectedEvent>(OnDisconnected);
// ES START
        SubscribeLocalEvent<CrewMonitoringServerComponent, ESUndergoDegradationEvent>(OnUndergoDegradation);
// ES END
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // check update rate
        _updateDiff += frameTime;
        if (_updateDiff < UpdateRate)
            return;
        _updateDiff -= UpdateRate;

        var servers = EntityQueryEnumerator<CrewMonitoringServerComponent>();

        while (servers.MoveNext(out var id, out var server))
        {
            if (!_singletonServerSystem.IsActiveServer(id))
                continue;

            UpdateTimeout(id);
            BroadcastSensorStatus(id, server);
        }
    }

    /// <summary>
    /// Adds or updates a sensor status entry if the received package is a sensor status update
    /// </summary>
    private void OnPacketReceived(EntityUid uid, CrewMonitoringServerComponent component, DeviceNetworkPacketEvent args)
    {
        var sensorStatus = _sensors.PacketToSuitSensor(args.Data);
        if (sensorStatus == null)
            return;

        sensorStatus.Timestamp = _gameTiming.CurTime;
        component.SensorStatus[args.SenderAddress] = sensorStatus;
    }

    /// <summary>
    /// Clears the servers sensor status list
    /// </summary>
    private void OnRemove(EntityUid uid, CrewMonitoringServerComponent component, ComponentRemove args)
    {
        component.SensorStatus.Clear();
    }

    /// <summary>
    /// Drop the sensor status if it hasn't been updated for to long
    /// </summary>
    private void UpdateTimeout(EntityUid uid, CrewMonitoringServerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        foreach (var (address, sensor) in component.SensorStatus)
        {
            var dif = _gameTiming.CurTime - sensor.Timestamp;
            if (dif.Seconds > component.SensorTimeout)
                component.SensorStatus.Remove(address);
        }
    }

    /// <summary>
    /// Broadcasts the status of all connected sensors
    /// </summary>
    private void BroadcastSensorStatus(EntityUid uid, CrewMonitoringServerComponent? serverComponent = null, DeviceNetworkComponent? device = null)
    {
        if (!Resolve(uid, ref serverComponent, ref device))
            return;

        var payload = new NetworkPayload()
        {
            [DeviceNetworkConstants.Command] = DeviceNetworkConstants.CmdUpdatedState,
            [SuitSensorConstants.NET_STATUS_COLLECTION] = serverComponent.SensorStatus
        };

        _deviceNetworkSystem.QueuePacket(uid, null, payload, device: device);
    }

    /// <summary>
    /// Clears sensor data on disconnect
    /// </summary>
    private void OnDisconnected(EntityUid uid, CrewMonitoringServerComponent component, ref DeviceNetServerDisconnectedEvent _)
    {
        component.SensorStatus.Clear();
    }
// ES START
    private void OnUndergoDegradation(Entity<CrewMonitoringServerComponent> ent, ref ESUndergoDegradationEvent args)
    {
        if (Transform(ent).GridUid is not { } grid)
            return;

        var sensors = new HashSet<Entity<SuitSensorComponent>>();
        _entityLookup.GetGridEntities(grid, sensors);

        foreach (var sensor in sensors)
        {
            // Don't change the sensor of clothing that doesn't support having it changed back
            if (sensor.Comp.ControlsLocked)
                continue;

            // Don't enable disabled sensors. First because it'll expose stealthy people and dead bodies, second because it doesnt make sense.
            if (sensor.Comp.Mode == SuitSensorMode.SensorOff)
                continue;
            _sensors.SetSensor(sensor.AsNullable(), SuitSensorMode.SensorBinary);
        }

        args.Handled = true;
    }
// ES END
}
