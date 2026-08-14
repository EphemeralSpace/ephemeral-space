using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Chat.Components;

/// <summary>
/// Component automatically added to processor entities associated with <see cref="ESChatChannelPrototype"/>.
/// Processors entities are essentially pipelines which operate on chat messages in order to transform them in various ways.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESChatProcessorComponent : Component
{
    /// <summary>
    /// The channel associated with this processor.
    /// Is automatically filled when the processor is spawned.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<ESChatChannelPrototype> Channel;
}
