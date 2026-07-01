es-objective-summary-fmt = {$name}: {$success ->
    [true] [color=limegreen]Success[/color]
    *[false] [color=red]Failed[/color]
} {$percent ->
    [0] {""}
    [100] {""}
    *[other] ([color=gray]{$percent}%[/color])
}

es-objective-text-troupe = Team
es-objective-tooltip-troupe = This is a [bold]shared organization objective[/bold].

    All members of your organization share this objective, and must work together. Objective completion is shared between everyone who has it assigned.

es-objective-text-secret-identity = Solo
es-objective-tooltip-secret-identity = This is a [bold]personal secret identity objective[/bold].

    This is a unique objective based on the secret identity that you are assigned. Only you can view this objective. Other members of your organization may have different personal objectives.
