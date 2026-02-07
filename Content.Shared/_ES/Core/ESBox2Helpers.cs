namespace Content.Shared._ES.Core;

public static class ESBox2Helpers
{
    public static float MinDimension(this Box2 box)
    {
        return Math.Min(box.Width, box.Height);
    }
}
