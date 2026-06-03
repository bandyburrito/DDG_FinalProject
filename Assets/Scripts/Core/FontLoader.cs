using UnityEngine;

/// <summary>
/// Loads JetBrains Mono once and caches the Font references. Every GUIStyle in
/// PlaceholderUI assigns .font from here so the whole game reads in one consistent
/// monospace voice.
///
/// Drop-in path is Assets/Resources/Fonts/{Regular,Bold}.ttf — Resources.Load wraps
/// the .ttf into a Font asset automatically, no manual TextMeshPro setup needed
/// (we're using IMGUI, which takes raw Font assets directly).
/// </summary>
public static class FontLoader
{
    private static Font _regular;
    private static Font _bold;
    private static bool _warnedRegular;
    private static bool _warnedBold;

    public static Font Regular
    {
        get
        {
            if (_regular == null) _regular = Resources.Load<Font>("Fonts/JetBrainsMono-Regular");
            if (_regular == null && !_warnedRegular)
            {
                Debug.LogWarning("[FontLoader] Missing Resources/Fonts/JetBrainsMono-Regular.ttf — falling back to default.");
                _warnedRegular = true;
            }
            return _regular;
        }
    }

    public static Font Bold
    {
        get
        {
            if (_bold == null) _bold = Resources.Load<Font>("Fonts/JetBrainsMono-Bold");
            if (_bold == null && !_warnedBold)
            {
                Debug.LogWarning("[FontLoader] Missing Resources/Fonts/JetBrainsMono-Bold.ttf — falling back to default.");
                _warnedBold = true;
            }
            // Fall back to Regular if Bold isn't shipped — better than reverting to Arial
            return _bold != null ? _bold : Regular;
        }
    }
}
