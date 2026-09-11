using System;
using UnityEngine;

public sealed class StrikeInput
{
    private readonly string preferenceKey;
    private int blockedThroughFrame = -1;
    public KeyCode Key { get; private set; }
    public StrikeInput(string preferenceKey = "OctOpus.Input.StrikeKey")
    {
        this.preferenceKey = preferenceKey;
        var saved = (KeyCode)PlayerPrefs.GetInt(preferenceKey, (int)KeyCode.Space);
        Key = IsBindable(saved) ? saved : KeyCode.Space;
    }
    public static bool IsBindable(KeyCode key) => key != KeyCode.Escape && key >= KeyCode.Backspace &&
        key < KeyCode.Mouse0 && Enum.IsDefined(typeof(KeyCode), key);
    public void SetKey(KeyCode key, int frame)
    {
        if (!IsBindable(key)) throw new ArgumentOutOfRangeException(nameof(key));
        Key = key;
        PlayerPrefs.SetInt(preferenceKey, (int)key);
        PlayerPrefs.Save();
        UpdateFocus(frame);
    }
    public void UpdateFocus(int frame) { blockedThroughFrame = frame + 1; }
    public bool CanStrike(bool focused, int frame, bool uiHasKeyboardFocus, bool rebinding) =>
        focused && frame > blockedThroughFrame && !uiHasKeyboardFocus && !rebinding;
}
