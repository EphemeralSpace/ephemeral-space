using System.Diagnostics.CodeAnalysis;
using Content.Shared._ES.Chat.SpeechVerb.Components;
using Content.Shared.Inventory;
using Content.Shared.Speech;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._ES.Chat.SpeechVerb;

public sealed partial class ESSpeechVerbSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;

    public static readonly ProtoId<SpeechVerbPrototype> DefaultSpeechVerb = "Default";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESSpeechVerbChatChannelComponent, ESGetChatMessageFormatEvent>(OnGetFormat);
    }

    private void OnGetFormat(Entity<ESSpeechVerbChatChannelComponent> ent, ref ESGetChatMessageFormatEvent args)
    {
        var verbPrototype = GetVerbPrototype(args.Source, args.Content);
        var verb = Loc.GetString(_random.Pick(verbPrototype.SpeechVerbStrings));

        // TODO: this formatting doesn't persist to the chat bubble.
        var fmt = verbPrototype.Bold ? ent.Comp.BoldFormat : ent.Comp.Format;

        args.Format = Loc.GetString(fmt, ("verb", verb));
        args.FontSize = verbPrototype.FontSize;
        args.Font = verbPrototype.FontId;
    }

    // TODO: SpeechVerbPrototype really sucks and this is what need to happen to make it good
    // - Create an enum for each different type of "sub-verb" (entries in SuffixSpeechVerb)
    // - Create a new data type that holds a dict of these to speech verb prototypes
    // - Pass around this new data type instead of raw speech verb prototype
    // - Add a method which gets the actual verb out of this class by passing in a string
    // The current system is really bad because having an overriden speech verb is mutually exclusive
    // with having the speech verb be based on string suffixes (WHICH SUCKS ASSSSSSSSSSS)
    // So i'd like to remedy this at some point because it'll be a massive pain whenever we have more
    // diversity in speech verbs.
    public SpeechVerbPrototype GetVerbPrototype(EntityUid source, string content)
    {
        var ev = new ESGetSpeechVerbEvent();
        RaiseLocalEvent(source, ref ev);

        if (ev.Handled)
            return _prototype.Index(ev.Verb);

        return GetVerbFromMessage(source, content);
    }

    public SpeechVerbPrototype GetVerbFromMessage(Entity<SpeechComponent?> ent, string content)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return _prototype.Index(DefaultSpeechVerb);

        // check for a suffix-applicable speech verb
        SpeechVerbPrototype? current = null;
        foreach (var (str, id) in ent.Comp.SuffixSpeechVerbs)
        {
            var proto = _prototype.Index(id);
            if (content.EndsWith(Loc.GetString(str)) && proto.Priority >= (current?.Priority ?? 0))
            {
                current = proto;
            }
        }

        // if no applicable suffix verb return the normal one used by the entity
        return current ?? _prototype.Index(ent.Comp.SpeechVerb);
    }
}

[ByRefEvent]
public record ESGetSpeechVerbEvent : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;

    public ProtoId<SpeechVerbPrototype>? Verb;

    [MemberNotNullWhen(true, nameof(Verb))]
    public bool Handled => Verb.HasValue;
}
