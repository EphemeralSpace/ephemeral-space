namespace Content.Shared._ES.Core.Timer;

/// <summary>
///     Used to allow a non-networked timer event to exist in shared and pass tests.
///     Has no functionality besides that.
/// </summary>
public sealed class NonNetworkedTimerEventAttribute : Attribute
{
}
