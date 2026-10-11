namespace WukongBench.Infrastructure.Input;

using System.Runtime.InteropServices;
using WukongBench.Domain.Abstractions;
using WukongBench.Domain.Models;
using WukongBench.Infrastructure.Interop;

/// <summary>
/// Drives the benchmark window with synthesised Win32 input
/// (<c>SendInput</c> / <c>SetCursorPos</c>).
///
/// The main menu of the benchmark tool only starts reacting to the keyboard once
/// the mouse has hovered it, which is why <see cref="UiScript"/> supports the
/// <see cref="UiActionKind.MouseMoveAndClick"/> action.
/// </summary>
public sealed class Win32KeyboardUiAutomation : IKeyboardUiAutomation
{
    private readonly IToolLogger _logger;

    public Win32KeyboardUiAutomation(IToolLogger logger)
    {
        _logger = logger;
    }

    public async Task<bool> RunAsync(IBenchmarkSession session, UiScript script, CancellationToken cancellationToken)
    {
        if (script.Steps.Count == 0)
        {
            return true;
        }

        if (!TryFocus(session))
        {
            _logger.Warning($"Could not bring '{session.WindowTitle}' to the foreground; input may be ignored.");
        }

        foreach (var step in script.Steps)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _logger.Info($"UI: {step.Description}");

            switch (step.Action)
            {
                case UiActionKind.KeyPress when step.Key != VirtualKey.None:
                    PressKey(step.Key, step.HoldMilliseconds);
                    break;

                case UiActionKind.MouseMoveAndClick:
                    MoveAndClick(session, step.NormalizedX, step.NormalizedY, step.HoldMilliseconds);
                    break;

                case UiActionKind.Wait:
                default:
                    break;
            }

            if (step.DelayAfterMilliseconds > 0)
            {
                await Task.Delay(step.DelayAfterMilliseconds, cancellationToken);
            }
        }

        return true;
    }

    private bool TryFocus(IBenchmarkSession session)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            if (session.TryActivateWindow())
            {
                return true;
            }

            Thread.Sleep(400);
        }

        return false;
    }

    private void PressKey(VirtualKey key, int holdMilliseconds)
    {
        var inputs = new[]
        {
            CreateKeyboardInput(key, keyUp: false),
            CreateKeyboardInput(key, keyUp: true),
        };

        if (holdMilliseconds > 0 && holdMilliseconds < 2000)
        {
            SendInput(new[] { inputs[0] });
            Thread.Sleep(holdMilliseconds);
            SendInput(new[] { inputs[1] });
            return;
        }

        SendInput(inputs);
    }

    private void MoveAndClick(IBenchmarkSession session, double normalizedX, double normalizedY, int holdMilliseconds)
    {
        if (session.MainWindowHandle == IntPtr.Zero)
        {
            _logger.Warning("The benchmark window handle is not available - skipping the mouse action.");
            return;
        }

        if (!NativeMethods.GetClientRect(session.MainWindowHandle, out var rect))
        {
            _logger.Warning("GetClientRect failed - skipping the mouse action.");
            return;
        }

        var origin = new NativeMethods.Point { X = 0, Y = 0 };
        NativeMethods.ClientToScreen(session.MainWindowHandle, ref origin);

        var x = origin.X + (int)Math.Round(rect.Width * Math.Clamp(normalizedX, 0, 1));
        var y = origin.Y + (int)Math.Round(rect.Height * Math.Clamp(normalizedY, 0, 1));

        _logger.Debug($"Cursor -> ({x}, {y}) inside a {rect.Width}x{rect.Height} client area.");
        NativeMethods.SetCursorPos(x, y);
        Thread.Sleep(Math.Max(120, holdMilliseconds));

        SendInput(new[]
        {
            CreateMouseInput(NativeMethods.MouseEventLeftDown),
        });
        Thread.Sleep(Math.Max(40, holdMilliseconds));
        SendInput(new[]
        {
            CreateMouseInput(NativeMethods.MouseEventLeftUp),
        });
    }

    private static NativeMethods.Input CreateKeyboardInput(VirtualKey key, bool keyUp) =>
        new()
        {
            Type = NativeMethods.InputKeyboardType,
            Union = new NativeMethods.InputUnion
            {
                Keyboard = new NativeMethods.InputKeyboard
                {
                    VirtualKey = (ushort)key,
                    Flags = keyUp ? NativeMethods.KeyEventKeyUp : 0,
                },
            },
        };

    private static NativeMethods.Input CreateMouseInput(uint flags) =>
        new()
        {
            Type = NativeMethods.InputMouseType,
            Union = new NativeMethods.InputUnion
            {
                Mouse = new NativeMethods.InputMouse
                {
                    Flags = flags,
                },
            },
        };

    private void SendInput(NativeMethods.Input[] inputs)
    {
        var size = Marshal.SizeOf<NativeMethods.Input>();
        var sent = NativeMethods.SendInput((uint)inputs.Length, inputs, size);
        if (sent != inputs.Length)
        {
            _logger.Debug($"SendInput delivered {sent}/{inputs.Length} events (win32 error {Marshal.GetLastWin32Error()}).");
        }
    }
}
