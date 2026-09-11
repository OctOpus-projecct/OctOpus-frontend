using System;
using UnityEngine;

public sealed class MovementInput
{
    public static readonly Rect PanelRect = new Rect(20, 20, 340, 480);
    private readonly string preferenceKey;
    private int blockedThroughFrame = -1;
    public int Button { get; private set; }

    public MovementInput(string preferenceKey = "OctOpus.Input.MoveMouseButton")
    {
        this.preferenceKey = preferenceKey;
        int saved = PlayerPrefs.GetInt(preferenceKey, 0);
        Button = saved >= 0 && saved <= 2 ? saved : 0;
    }

    public void SetButton(int button)
    {
        if (button < 0 || button > 2) throw new ArgumentOutOfRangeException(nameof(button));
        Button = button;
        PlayerPrefs.SetInt(preferenceKey, button);
        PlayerPrefs.Save();
    }

    public void UpdateFocus(bool focused, int frame)
    {
        // Unity may report focus before or after this frame's Update.
        // Suppress the following frame too so the activation click never moves.
        blockedThroughFrame = frame + 1;
    }

    public bool CanMove(Vector2 screenPoint, int screenHeight, bool focused, int frame, bool blockLegacyPanel = true)
    {
        var guiPoint = new Vector2(screenPoint.x, screenHeight - screenPoint.y);
        return focused && frame > blockedThroughFrame && (!blockLegacyPanel || !PanelRect.Contains(guiPoint));
    }
}
