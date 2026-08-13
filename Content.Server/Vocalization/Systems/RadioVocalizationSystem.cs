using System.Linq;
using Content.Server._ES.Chat;
using Content.Server.Vocalization.Components;
using Content.Shared._ES.Chat;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Vocalization.Systems;

/// <summary>
/// RadioVocalizationSystem handles vocalizing things via equipped radios when a VocalizeEvent is fired
/// </summary>
public sealed partial class RadioVocalizationSystem : EntitySystem
{
    [Dependency] private ESChatSystem _chat = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;

    private static readonly ProtoId<ESChatChannelFilterPrototype> RadioFilter = "Radio";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadioVocalizerComponent, VocalizeEvent>(OnVocalize);
    }

    /// <summary>
    /// Called whenever an entity with a VocalizerComponent tries to speak
    /// </summary>
    private void OnVocalize(Entity<RadioVocalizerComponent> entity, ref VocalizeEvent args)
    {
        if (args.Handled)
            return;

        // set to handled if we succeed in speaking on the radio
        args.Handled = TrySpeakRadio(entity.Owner, args.Message);
    }

    /// <summary>
    /// Selects a random radio channel from all ActiveRadio entities in a given entity's inventory
    /// If no channels are found, this returns false and sets channel to an empty string
    /// </summary>
    private bool TryPickRandomRadioChannel(EntityUid entity, out ProtoId<ESChatChannelPrototype> channel)
    {
        channel = default;

        var channels = _chat.GetPermittedChannels(entity)
            .Select(_proto.Index)
            .Where(p => p.FilterCategory == RadioFilter)
            .ToList();

        if (channels.Count == 0)
            return false;

        channel = _random.Pick(channels);
        return true;
    }

    /// <summary>
    /// Attempts to speak on the radio. Returns false if there is no radio or talking on radio fails somehow
    /// </summary>
    /// <param name="entity">Entity to try and make speak on the radio</param>
    /// <param name="message">Message to speak</param>
    private bool TrySpeakRadio(Entity<RadioVocalizerComponent?> entity, string message)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        if (!_random.Prob(entity.Comp.RadioAttemptChance))
            return false;

        if (!TryPickRandomRadioChannel(entity, out var channel))
            return false;

        _chat.TrySendMessage(
            message,
            channel,
            entity);

        return true;
    }
}
