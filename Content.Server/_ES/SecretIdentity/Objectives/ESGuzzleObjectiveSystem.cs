using Content.Server._ES.SecretIdentity.Objectives.Components;
using Content.Server._ES.SecretIdentity.Objectives.Relays;
using Content.Server._ES.SecretIdentity.Objectives.Relays.Components;
using Content.Shared._ES.Objectives;

namespace Content.Server._ES.SecretIdentity.Objectives;

/// <summary>
///     This handles a particular kind of objective that requires imbibing X amount of reagents, total.
/// </summary>
/// <seealso cref="ESGuzzleObjectiveComponent"/>
public sealed class ESGuzzleObjectiveSystem : ESBaseObjectiveSystem<ESGuzzleObjectiveComponent>
{
    public override Type[] RelayComponents => new[] { typeof(ESMuncherRelayComponent) };

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESGuzzleObjectiveComponent, ESBodyIngestingEvent>(OnBodyIngesting);
    }

    private void OnBodyIngesting(Entity<ESGuzzleObjectiveComponent> ent, ref ESBodyIngestingEvent args)
    {
        if (!args.IsDrink)
            return; // We're NOT guzzling.

        if (args.FoodSolution == null)
            return;

        // Tally our guzzling.
        ObjectivesSys.AdjustObjectiveCounter(ent.Owner, args.FoodSolution.Volume.Float());
    }
}
