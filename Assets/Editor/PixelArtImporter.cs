#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Auto-applies pixel art import settings to any PNG dropped into
/// Resources/Tiles/ or Resources/Entities/ — point filter, single
/// sprite (no auto-slicing), no compression. Runtime SpriteLoader
/// builds the actual sprite with its own pivot + PPU.
/// </summary>
public class PixelArtImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        // Only target our project folders
        if (!assetPath.Contains("/Resources/Tiles/") && !assetPath.Contains("/Resources/Entities/"))
            return;

        var importer = (TextureImporter)assetImporter;

        importer.textureType         = TextureImporterType.Sprite;
        importer.spriteImportMode    = SpriteImportMode.Single;        // no auto-slicing
        importer.filterMode          = FilterMode.Point;
        importer.textureCompression  = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = 16f;
        importer.alphaIsTransparency = true;
        importer.isReadable          = true;
        importer.mipmapEnabled       = false;
        importer.wrapMode            = TextureWrapMode.Clamp;

        // Pivot: entities use bottom-center, tiles use top-face-center
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = assetPath.Contains("/Resources/Entities/")
            ? new Vector2(0.5f, 0f)      // entities: feet at bottom-center
            : new Vector2(0.5f, 0.75f);  // tiles: top-face center
        importer.SetTextureSettings(settings);

        Debug.Log($"[PixelArtImporter] Configured {assetPath} (pivot {settings.spritePivot}, PPU 16, Point)");
    }
}
#endif
