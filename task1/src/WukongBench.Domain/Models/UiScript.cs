namespace WukongBench.Domain.Models;

/// <summary>
/// One scripted interaction with the benchmark tool window.
/// </summary>
public sealed class UiStep
{
    public UiStep(
        UiActionKind action,
        VirtualKey key,
        double normalizedX,
        double normalizedY,
        int holdMilliseconds,
        int delayAfterMilliseconds,
        string description)
    {
        Action = action;
        Key = key;
        NormalizedX = normalizedX;
        NormalizedY = normalizedY;
        HoldMilliseconds = holdMilliseconds;
        DelayAfterMilliseconds = delayAfterMilliseconds;
        Description = description;
    }

    /// <summary>What the step does.</summary>
    public UiActionKind Action { get; }

    /// <summary>Key to press for <see cref="UiActionKind.KeyPress"/>.</summary>
    public VirtualKey Key { get; }

    /// <summary>Horizontal position inside the client area, 0 = left edge, 1 = right edge.</summary>
    public double NormalizedX { get; }

    /// <summary>Vertical position inside the client area, 0 = top edge, 1 = bottom edge.</summary>
    public double NormalizedY { get; }

    /// <summary>How long a key is held down / how long the mouse button stays pressed.</summary>
    public int HoldMilliseconds { get; }

    /// <summary>How long to wait after the action before the next step.</summary>
    public int DelayAfterMilliseconds { get; }

    /// <summary>Explanation printed in the console log.</summary>
    public string Description { get; }
}

/// <summary>
/// A named sequence of UI steps. The "how to drive the Wukong benchmark UI"
/// knowledge lives in the Application layer, the key/mouse sending mechanism in
/// the Infrastructure layer, and this model just carries the data between them.
/// </summary>
public sealed class UiScript
{
    public UiScript(string name, string description, IReadOnlyList<UiStep> steps)
    {
        Name = name;
        Description = description;
        Steps = steps;
    }

    public string Name { get; }
    public string Description { get; }
    public IReadOnlyList<UiStep> Steps { get; }
}
