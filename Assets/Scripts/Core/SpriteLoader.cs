using UnityEngine;

/// <summary>
/// Robust sprite loader that tries multiple paths.
/// PNGs in Assets/Resources/Tiles/ and Assets/Resources/Entities/
/// are accepted regardless of whether Unity auto-imported them as
/// Texture or Sprite type.
/// </summary>
public static class SpriteLoader
{
    public const float PIXELS_PER_UNIT = 16f;

    public static Sprite LoadTile(string name) =>
        BuildSprite($"Tiles/{name}", new Vector2(0.5f, 0.75f));

    public static Sprite LoadEntity(string name) =>
        BuildSprite($"Entities/{name}", new Vector2(0.5f, 0f));

    public static Sprite LoadOverlay(string name) =>
        BuildSprite($"Tiles/{name}", new Vector2(0.5f, 0.75f));

    /// <summary>
    /// Resolves a Resources path to a Texture2D regardless of Unity's
    /// auto-import settings, then builds a fresh Sprite with our pivot/PPU.
    /// </summary>
    static Sprite BuildSprite(string resourcePath, Vector2 pivot)
    {
        Texture2D tex = Resources.Load<Texture2D>(resourcePath);

        // Unity sometimes imports PNGs as Sprite type, which can hide them from Texture2D loads.
        // Fall back to loading the Sprite and grabbing its texture.
        if (tex == null)
        {
            var existingSprite = Resources.Load<Sprite>(resourcePath);
            if (existingSprite != null) tex = existingSprite.texture;
        }

        if (tex == null)
        {
            Debug.LogWarning($"[SpriteLoader] Missing: Assets/Resources/{resourcePath}.png");
            return null;
        }

        // filterMode change is safe even for non-readable textures (it's metadata)
        tex.filterMode = FilterMode.Point;

        Debug.Log($"[SpriteLoader] Loaded {resourcePath} ({tex.width}x{tex.height})");

        return Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            pivot,
            PIXELS_PER_UNIT);
    }
}
