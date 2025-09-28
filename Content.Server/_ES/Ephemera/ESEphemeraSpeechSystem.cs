using System.Diagnostics;
using System.Linq;
using Content.Server._ES.Ephemera.Components;
using Content.Server.Chat.Systems;
using Content.Shared.Interaction;
using Robust.Server.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._ES.Ephemera;

public sealed class ESEphemeraSpeechSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    // This is only meant to be like an anti-spam system, so it doesn't have to
    // really reflect how long it would take a person to read the dialogue.
    // It only has to serve as a *minimum* value.
    public const float DelaySecondPerWords = 0.14f;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESEphemeraSpeakerComponent, ActivateInWorldEvent>(OnActivateInWorld);

        SubscribeLocalEvent<ESEphemeraSequentialDialogueComponent, ESEphemeraGetDialogueEvent>(OnSequentialGetDialogue);
        SubscribeLocalEvent<ESEphemeraRandomDialogueComponent, ESEphemeraGetDialogueEvent>(OnRandomGetDialogue);
    }

    private void OnActivateInWorld(Entity<ESEphemeraSpeakerComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TrySpeakDialogue(ent);
    }

    private void OnSequentialGetDialogue(Entity<ESEphemeraSequentialDialogueComponent> ent, ref ESEphemeraGetDialogueEvent args)
    {
        if (args.Handled)
            return;

        if (!ent.Comp.Dialogue.TryGetValue(ent.Comp.DialogueIndex, out var dialogueId))
            return;

        args.Line = Loc.GetString(dialogueId);
        ent.Comp.DialogueResetTime = _timing.CurTime + GetDialogueSpeakLength(args.Line) + ent.Comp.DialogueResetDelay;

        // Increment index so we read the next line
        SetDialogueIndex(ent, ent.Comp.DialogueIndex + 1);
    }

    private void OnRandomGetDialogue(Entity<ESEphemeraRandomDialogueComponent> ent, ref ESEphemeraGetDialogueEvent args)
    {
        if (args.Handled)
            return;

        var dataset = _prototype.Index(ent.Comp.Dialogue);
        if (dataset.Values.Count == 0)
            return;

        var lines = ent.Comp.LastDialogue.HasValue && dataset.Values.Count > 1
            ? dataset.Values.Except([ent.Comp.LastDialogue.Value.ToString()]).ToList()
            : dataset.Values.ToList();
        var lineId = _random.Pick(lines);
        args.Line = Loc.GetString(lineId);
        ent.Comp.LastDialogue = lineId;
    }

    /// <summary>
    /// Attempts to speak the current dialogue, returning false if unable to.
    /// </summary>
    public bool TrySpeakDialogue(Entity<ESEphemeraSpeakerComponent> ent)
    {
        if (!CanSpeakDialogue(ent))
            return false;

        SpeakDialogue(ent);
        return true;
    }

    /// <summary>
    /// Returns if the current entity is capable of speaking.
    /// </summary>
    public bool CanSpeakDialogue(Entity<ESEphemeraSpeakerComponent> ent)
    {
        return _timing.CurTime > ent.Comp.NextCanSpeakTime;
    }

    private void SpeakDialogue(Entity<ESEphemeraSpeakerComponent> ent)
    {
        var ev = new ESEphemeraGetDialogueEvent();
        RaiseLocalEvent(ent, ref ev);
        if (!ev.Handled)
            return;

        // Send the current dialogue line into chat.
        var message = ev.Line!;
        _chat.TrySendInGameICMessage(ent, message, InGameICChatType.Speak, ChatTransmitRange.GhostRangeLimit, hideLog: true, ignoreActionBlocker: true);

        // Play talk sound effect
        // TODO: associate this with message length
        _audio.PlayPvs(ent.Comp.SpeakSound, ent, ent.Comp.SpeakSound?.Params.WithVariation(0.125f));

        // Prevent sending next dialogue until the player has approximately read it (anti-spam)
        // Also refresh the counter for resetting the dialogue index.
        var speakLength = GetDialogueSpeakLength(message);
        ent.Comp.NextCanSpeakTime = _timing.CurTime + speakLength;
    }

    /// <summary>
    /// Sets the dialogue index to a certain value, ensuring that the new value is always valid.
    /// </summary>
    public void SetDialogueIndex(Entity<ESEphemeraSequentialDialogueComponent> ent, int index)
    {
        Debug.Assert(ent.Comp.Dialogue.Count > 0); // This will break if no dialogue is present.
        ent.Comp.DialogueIndex = index % ent.Comp.Dialogue.Count;
    }

    public static TimeSpan GetDialogueSpeakLength(string line)
    {
        var wordCount = line.Split(" ").Length; // Not the most efficient way of doing this, but good enough
        var speakLength = TimeSpan.FromSeconds(wordCount * DelaySecondPerWords);
        return speakLength;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ESEphemeraSequentialDialogueComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.DialogueIndex == 0)
                continue;
            if (_timing.CurTime < comp.DialogueResetTime)
                continue;
            SetDialogueIndex((uid, comp), 0);
        }
    }
}
