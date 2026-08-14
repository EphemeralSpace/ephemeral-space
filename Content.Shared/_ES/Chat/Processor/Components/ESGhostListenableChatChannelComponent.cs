using Content.Shared._ES.Chat.Components;
using Content.Shared.Ghost;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Chat.Processor.Components;

/// <summary>
/// <see cref="ESChatProcessorComponent"/> for a chat channel which is globally received by all entities with <see cref="GhostHearingComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESGhostListenableChatChannelSystem))]
public sealed partial class ESGhostListenableChatChannelComponent : Component;
