namespace Content.Shared._ES.Core;

public static class ESAngleHelpers
{
    /// <summary>
    /// Converts an angle to a "navigational angle" where true north is 0 degrees and as theta increases, it continues clockwise.
    /// </summary>
    public static Angle ToCompassAngle(this Angle angle)
    {
        var offsetAngle = Math.PI - angle.Theta;
        if (offsetAngle < 0)
            offsetAngle += Math.Tau;

        return new Angle(offsetAngle % Math.Tau);
    }
}
