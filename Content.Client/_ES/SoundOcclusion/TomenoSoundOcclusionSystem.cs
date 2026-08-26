using Content.Client._ES.LocalPlayer;
using Content.Shared.Atmos.Components;

namespace Content.Client._ES.SoundOcclusion;

// mild notes
// - airtight does not network directional info rn (ie CurrentAirblockedDirection will always be 0 on the client, if you need to check it just check InitialAirblockedDirection)
// (i would just ignore directional stuff probably its an annoying case to handle and not meaningful i think)
// - airtight status changing wont be predicted with stuff like doors so they'll be slightly off until it actually updates
// cuz i didnt move airtight system stuff into shared thats more of an actual endeavor
// - maps will need to be per-grid id see how ExplosionSystem.Airtight.cs etc does it or SpreaderSystem i think also does it
// - for cases when not grid attached (space) idk. just dont pathfind. who cares

public sealed class TomenoSoundOcclusionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AirtightComponent, ComponentInit>(OnAirtightInit);
        SubscribeLocalEvent<AirtightComponent, ComponentShutdown>(OnAirtightShutdown);
        // for all intents and purposes an unanchor = remove and anchor = add
        SubscribeLocalEvent<AirtightComponent, AnchorStateChangedEvent>(OnAirtightAnchorChange);
        SubscribeLocalEvent<AirtightComponent, ReAnchorEvent>(OnAirtightReAnchor);
        SubscribeLocalEvent<AirtightComponent, MoveEvent>(OnAirtightMove);
        SubscribeLocalEvent<AirtightComponent, AfterAutoHandleStateEvent>(OnAirtightStateChange);

        SubscribeLocalEvent<ESLocalPlayerMarkerComponent, MoveEvent>(OnPlayerMove);
    }

    private void OnAirtightInit(Entity<AirtightComponent> ent, ref ComponentInit args)
    {
        // add to grid map
    }

    private void OnAirtightShutdown(Entity<AirtightComponent> ent, ref ComponentShutdown args)
    {
        // remove from grid map
    }

    private void OnAirtightAnchorChange(Entity<AirtightComponent> ent, ref AnchorStateChangedEvent args)
    {
        // add/remove from grid map
    }

    private void OnAirtightReAnchor(Entity<AirtightComponent> ent, ref ReAnchorEvent args)
    {
        // remove from old grid map and add to new grid map
    }

    private void OnAirtightMove(Entity<AirtightComponent> ent, ref MoveEvent args)
    {
        // you get the idea
    }

    private void OnAirtightStateChange(Entity<AirtightComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        // add/remove depending on whether ent.Comp.AirBlocked is true/false now relative to its status on the grid map
        // you get the idea
    }

    private void OnPlayerMove(Entity<ESLocalPlayerMarkerComponent> ent, ref MoveEvent args)
    {
        // idk whatever you need this for
    }
}
