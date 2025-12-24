using System.Linq;
using Content.Shared.Camera;
using Content.Shared.Movement.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._ES.Camera;

/// <summary>
///     Handles sending rotational or translational screenshake to an entity, managing the screenshake commands
///     of every entity currently screenshaking, and setting offset/rotation based on
/// </summary>
public sealed class ESSharedScreenshakeSystem : EntitySystem
{
    [Dependency] private readonly SharedContentEyeSystem _contentEye = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    #region Internal

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESScreenshakeComponent, ESGetEyeRotationEvent>(OnGetEyeRotation);
        SubscribeLocalEvent<ESScreenshakeComponent, GetEyeOffsetEvent>(OnGetEyeOffset);
        SubscribeLocalEvent<ESScreenshakeComponent, EntityUnpausedEvent>(OnEntityUnpaused);
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

    public void UpdateScreenshakers(float frameTime)
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
        foreach (var command in ent.Comp.Commands)
        {
            var trauma =
                CalculateTraumaValueForCurrentTime(command.TranslationalTrauma, ent.Comp.TranslationalDecayRate, command.Start);
            if (trauma <= 0)
                continue;


        }
    }

    private void OnGetEyeRotation(Entity<ESScreenshakeComponent> ent, ref ESGetEyeRotationEvent args)
    {
        foreach (var command in ent.Comp.Commands)
        {
            var trauma =
                CalculateTraumaValueForCurrentTime(command.RotationalTrauma, ent.Comp.RotationalDecayRate, command.Start);
            if (trauma <= 0)
                continue;
        }
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
    private TimeSpan CalculateEndTimeForCommand(Entity<ESScreenshakeComponent> ent, float rotationalTrauma, float translationalTrauma, TimeSpan start)
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

    #endregion
}
