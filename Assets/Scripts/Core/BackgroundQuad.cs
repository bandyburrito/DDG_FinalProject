using UnityEngine;

/// <summary>
/// Renders a full-viewport background image behind everything in the scene. Spawns a
/// SpriteRenderer on a child object at a deep Z, with the lowest sorting order, sized
/// to always cover the camera's visible area (re-scales when the camera changes —
/// handles variable room sizes from FitCamera).
///
/// The image is loaded from Resources/Textures/background.png at Awake so it requires
/// no manual asset wiring.
/// </summary>
public class BackgroundQuad : MonoBehaviour
{
    private SpriteRenderer _sr;
    private Camera _cam;

    void Awake()
    {
        var tex = Resources.Load<Texture2D>("Textures/background");
        if (tex == null) { Debug.LogWarning("[BackgroundQuad] Missing Resources/Textures/background.png"); return; }

        tex.filterMode = FilterMode.Bilinear;   // smooth at large scale — it's a 1920×1080 painting, not pixel art

        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                   new Vector2(0.5f, 0.5f), 100f);   // 100 PPU = 1 unit per 100 px

        var go = new GameObject("BG_Quad");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0, 0, 50f);   // far behind everything

        _sr = go.AddComponent<SpriteRenderer>();
        _sr.sprite       = sprite;
        _sr.sortingOrder  = -999;   // behind every tile, highlight, and entity
        _sr.color         = Color.white;
    }

    void Start()
    {
        _cam = Camera.main;
        FitToCamera();
    }

    void LateUpdate()
    {
        // Re-fit every frame so it tracks camera position/zoom changes from FitCamera
        // (called each wave when the grid size changes). Cheap — just two divisions.
        FitToCamera();
    }

    void FitToCamera()
    {
        if (_sr == null || _cam == null) return;

        // Camera centre in world space — keep the quad centred on it.
        _sr.transform.position = new Vector3(
            _cam.transform.position.x,
            _cam.transform.position.y,
            _sr.transform.position.z);

        // Scale the sprite so it covers the full orthographic viewport + a small margin
        // (×1.05) so no bg-color seams appear at the edges even with rounding.
        float camH = _cam.orthographicSize * 2f;
        float camW = camH * _cam.aspect;

        var bounds = _sr.sprite.bounds.size;   // sprite's un-scaled world size
        float scaleX = (camW / bounds.x) * 1.05f;
        float scaleY = (camH / bounds.y) * 1.05f;
        float scale  = Mathf.Max(scaleX, scaleY);   // cover (not letterbox)

        _sr.transform.localScale = new Vector3(scale, scale, 1f);
    }
}
