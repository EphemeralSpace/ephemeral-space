using Content.IntegrationTests.Tests.Interaction;

namespace Content.IntegrationTests.Tests.Construction.Interaction;

public sealed class MachineConstruction : InteractionTest
{
    private const string MachineFrame = "MachineFrame";
    private const string Unfinished = "UnfinishedMachineFrame";
    private const string MicrowaveBoard = "MicrowaveMachineCircuitboard";
    private const string Microwave = "KitchenMicrowave";

    [Test]
    public async Task ConstructMicrowave()
    {
        await StartConstruction(MachineFrame);
        await InteractUsing(Steel, 5);
        ClientAssertPrototype(Unfinished, Target);
        await Interact(Wrench, Cable);
        AssertPrototype(MachineFrame);
        await Interact(MicrowaveBoard, Manipulator1, Glass, Glass, Cable, Cable, Screw);
        AssertPrototype(Microwave);
    }

    [Test, Ignore("this shit fucking doesnt work")]
    public async Task DeconstructMicrowave()
    {
        await StartDeconstruction(Microwave);
        await Interact(Pry);
        AssertPrototype(MachineFrame);
        await Interact(Pry, Cut);
        AssertPrototype(Unfinished);
        await Interact(Wrench, Screw);
        AssertDeleted();
        await AssertEntityLookup(
            (Steel, 5),
            (Cable, 2),
            (Glass, 2),
            (Manipulator1, 1),
            (MicrowaveBoard, 1));
    }

    [Test]
    public async Task ChangeMachine()
    {
        // Partially deconstruct a Microwave.
        await SpawnTarget(Microwave);
        await Interact(Screw, Pry, Pry);
        AssertPrototype(MachineFrame);

        // Change it into an autolathe
        await InteractUsing("AutolatheMachineCircuitboard");
        AssertPrototype(MachineFrame);
        await Interact(Manipulator1, Manipulator1, Manipulator1, Manipulator1, Glass, Screw);
        AssertPrototype("ESAutolathe");
    }
}

