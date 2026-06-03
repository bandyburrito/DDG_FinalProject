using UnityEngine;

/// <summary>
/// Centralised colour palette for the whole game. Pulled from the character + tile
/// colours already in use so menu chrome, panels, and gameplay highlights all read
/// as the same product rather than three different students' art.
///
/// Anchor logic:
///   • Camera background is dark purple (#211333) — extending that into a darker
///     base (BgMain/BgDeep) keeps the menu/grid transition seamless.
///   • Drone cyan (#40C0E0) and telegraph yellow (#FFD91A) already exist as
///     friendly/active accents — re-using them avoids a new colour budget.
///   • Enemy reds get a desaturated cousin (Danger) for game-over states.
/// </summary>
public static class Palette
{
    // ── Backgrounds ──────────────────────────────────────────────────────────
    public static readonly Color BgDeep   = Hex(0x14, 0x10, 0x1F);   // deepest panel/vignette
    public static readonly Color BgMain   = Hex(0x1A, 0x14, 0x28);   // primary menu/world bg
    public static readonly Color BgPanel  = Hex(0x2D, 0x25, 0x3D);   // cards, button base
    public static readonly Color BgHover  = Hex(0x4A, 0x3F, 0x63);   // hovered button fill

    // ── Text ─────────────────────────────────────────────────────────────────
    public static readonly Color Text     = Hex(0xF0, 0xEA, 0xD6);   // primary warm off-white
    public static readonly Color TextMute = Hex(0x8B, 0x85, 0xA0);   // footers, secondary
    public static readonly Color TextDim  = Hex(0x55, 0x50, 0x68);   // disabled / placeholder

    // ── Accents (re-used from existing gameplay tints) ───────────────────────
    public static readonly Color AccentYellow = Hex(0xFF, 0xD9, 0x1A);  // active turn, important
    public static readonly Color AccentCyan   = Hex(0x40, 0xC0, 0xE0);  // friendly highlight
    public static readonly Color Danger       = Hex(0xC0, 0x40, 0x50);  // game over, hostile
    public static readonly Color Success      = Hex(0x6A, 0xC8, 0x70);  // win, heal feedback

    private static Color Hex(int r, int g, int b, int a = 255) =>
        new Color(r / 255f, g / 255f, b / 255f, a / 255f);
}
