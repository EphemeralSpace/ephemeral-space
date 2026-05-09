es-objective-summary-fmt = {$name}: {$success ->
    [true] [color=limegreen]Success[/color]
    *[false] [color=red]Failed[/color]
} {$percent ->
    [0] {""}
    [100] {""}
    *[other] ([color=gray]{$percent}%[/color])
}

es-objective-text-troupe = Troupe
es-objective-tooltip-troupe = This is a [bold]shared objective[/bold].

    All members of your troupe must work together to complete it.

es-objective-text-mask = Mask
es-objective-tooltip-mask = This is a [bold]personal objective[/bold].

    You have to complete it yourself, although other can choose to help you.
