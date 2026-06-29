es-objective-summary-fmt = {$name}: {$success ->
    [true] [color=limegreen]Success[/color]
    *[false] [color=red]Failed[/color]
} {$percent ->
    [0] {""}
    [100] {""}
    *[other] ([color=gray]{$percent}%[/color])
}

es-objective-text-troupe = Team
es-objective-tooltip-troupe = This is a [bold]shared troupe objective[/bold].

    All members of your troupe share this objective, and must work together. Objective completion is shared between everyone who has it assigned.

es-objective-text-mask = Solo
es-objective-tooltip-mask = This is a [bold]personal mask objective[/bold].

    This is a unique objective based on the mask that you are assigned. Only you can view this objective. Other members of your troupe may have different personal objectives.
