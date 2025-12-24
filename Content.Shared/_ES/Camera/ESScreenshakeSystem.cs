using System.Linq;
using System.Numerics;
using Content.Shared.Camera;
using Content.Shared.Movement.Systems;
using Content.Shared.Random.Helpers;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Noise;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._ES.Camera;

/// <summary>
///     Handles sending rotational or translational screenshake to an entity, managing the screenshake commands
///     of every entity currently screenshaking, and setting offset/rotation based on
/// </summary>
public sealed class ESScreenshakeSystem : EntitySystem
{
    [Dependency] private readonly SharedContentEyeSystem _contentEye = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IConsoleHost _host = default!;

    #region Internal

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESScreenshakeComponent, ESGetEyeRotationEvent>(OnGetEyeRotation);
        SubscribeLocalEvent<ESScreenshakeComponent, GetEyeOffsetEvent>(OnGetEyeOffset);
        SubscribeLocalEvent<ESScreenshakeComponent, EntityUnpausedEvent>(OnEntityUnpaused);

        _host.RegisterCommand("screenshake", ScreenshakeCommand);
    }

    private void ScreenshakeCommand(IConsoleShell shell, string argStr, string[] args)
    {
        if (!float.TryParse(args[0], out var traum))
        {
            return;
        }

        if (shell.Player?.AttachedEntity is not { } ent)
            return;

        Screenshake(ent, traum, traum);
    }

    // frameupdate on client, tick update on server
    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        // don't think this can even happen but
        if (!_net.IsClient)
            return;

        UpdateScreenshakers(frameTime);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        UpdateScreenshakers(frameTime);
    }

    private void UpdateScreenshakers(float frameTime)
    {
        base.Update(frameTime);

        // TODO mirror might make sense to never remove individual commands and only remove the comp if theyre all > calculatedend instead.
        var shakers = EntityQueryEnumerator<EyeComponent, ESScreenshakeComponent>();
        while (shakers.MoveNext(out var ent, out var eye, out var shake))
        {
            if (shake.Commands.Count == 0)
            {
                RemCompDeferred<ESScreenshakeComponent>(ent);
                continue;
            }

            foreach (var command in shake.Commands.ToList())
            {
                // handle removing old commands
                if (_timing.CurTime < command.CalculatedEnd)
                    continue;
                shake.Commands.Remove(command);
                Dirty(ent, shake);
            }

            _contentEye.UpdateEyeOffset((ent, eye));
            _contentEye.UpdateEyeRotation((ent, eye));
        }
    }

    private void OnGetEyeOffset(Entity<ESScreenshakeComponent> ent, ref GetEyeOffsetEvent args)
    {
        if (!TryComp<EyeComponent>(ent, out var eye))
            return;

        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var noise = new FastNoiseLite(seed);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

        var accumulatedOffset = Vector2.Zero;
        var maxOffset = new Vector2(0.25f, 0.25f);
        foreach (var command in ent.Comp.Commands)
        {
            var trauma =
                CalculateTraumaValueForCurrentTime(command.TranslationalTrauma, ent.Comp.TranslationalDecayRate, command.Start);
            if (trauma <= 0)
                continue;

            var offsetX = (maxOffset.X * (trauma / 100f)) * noise.GetNoise((float) _timing.CurTime.TotalSeconds, 0f);
            noise.SetSeed(seed + 1);
            var offsetY = (maxOffset.Y * (trauma / 100f)) * noise.GetNoise((float) _timing.CurTime.TotalSeconds, 0f);
            accumulatedOffset += new Vector2(offsetX, offsetY);
        }

        args.Offset = eye.Offset + accumulatedOffset;
    }

    private void OnGetEyeRotation(Entity<ESScreenshakeComponent> ent, ref ESGetEyeRotationEvent args)
    {
        if (!TryComp<EyeComponent>(ent, out var eye))
            return;

        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var noise = new FastNoiseLite(seed);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

        // 20deg max
        var accumulatedAngle = Angle.Zero;
        var maxAngle = Angle.FromDegrees(20f);
        foreach (var command in ent.Comp.Commands)
        {
            var trauma =
                CalculateTraumaValueForCurrentTime(command.RotationalTrauma, ent.Comp.RotationalDecayRate, command.Start);
            if (trauma <= 0)
                continue;

            var angle = (maxAngle * (trauma / 100f)) * noise.GetNoise((float)_timing.CurTime.TotalSeconds, 0f);
            accumulatedAngle += new Angle(angle);
        }

        args.Rotation = eye.Rotation + accumulatedAngle;
    }

    private void OnEntityUnpaused(Entity<ESScreenshakeComponent> ent, ref EntityUnpausedEvent args)
    {
        // rebuild screenshake commands but with offset times
        var newSet = new HashSet<ESScreenshakeCommand>();
        foreach (var command in ent.Comp.Commands)
        {
            var newCommand = command with
            {
                CalculatedEnd = command.CalculatedEnd + args.PausedTime,
                Start = command.Start + args.PausedTime,
            };

            newSet.Add(newCommand);
        }

        ent.Comp.Commands = newSet;
        Dirty(ent);
    }

    /// <summary>
    ///     Calculates when both traumas will be at least = 0 given the decay rate and start time.
    /// </summary>
    private TimeSpan CalculateEndTimeForCommand(Entity<ESScreenshakeComponent> ent, float translationalTrauma, float rotationalTrauma, TimeSpan start)
    {
        // https://www.desmos.com/calculator/optip8eucx
        var secsUntilRotationalEnd = MathF.Sqrt(rotationalTrauma / ent.Comp.RotationalDecayRate);
        var secsUntilTranslationalEnd = MathF.Sqrt(translationalTrauma / ent.Comp.TranslationalDecayRate);
        var larger = secsUntilTranslationalEnd >= secsUntilRotationalEnd
            ? secsUntilTranslationalEnd
            : secsUntilRotationalEnd;

        return start + TimeSpan.FromSeconds(larger);
    }

    /// <summary>
    ///     Gets the trauma value for the current time, given the decay rate and start time.
    /// </summary>
    private float CalculateTraumaValueForCurrentTime(float startTrauma, float decay, TimeSpan start)
    {
        var timeDiff = _timing.CurTime - start;

        // erm
        if (timeDiff < TimeSpan.Zero)
            return 0f;

        // trauma decays quadratically with seconds passed
        // https://www.desmos.com/calculator/optip8eucx
        var totalSecsSquared = (float) (timeDiff.TotalSeconds * timeDiff.TotalSeconds);
        return -totalSecsSquared * decay + startTrauma;
    }

    #endregion

    #region Public API

    public void Screenshake(EntityUid uid, float translationalTrauma, float rotationalTrauma, float frequency = 0.01f)
    {
        if (!HasComp<EyeComponent>(uid))
            return;

        var comp = EnsureComp<ESScreenshakeComponent>(uid);
        var time = _timing.CurTime;
        var end = CalculateEndTimeForCommand((uid, comp), translationalTrauma, rotationalTrauma, time);
        var command = new ESScreenshakeCommand(translationalTrauma, rotationalTrauma, time, end, frequency);

        comp.Commands.Add(command);
        Dirty(uid, comp);
    }

    #endregion
}
