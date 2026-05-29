using UnityEngine;

/// <summary>
/// Tiny HP bar that sits above an Entity. Auto-builds its own background and
/// foreground sprites at runtime — no prefab or Inspector setup needed.
/// Shrinks from right to left as HP drops, and colour-shifts green → yellow → red.
/// </summary>
[RequireComponent(typeof(Entity))]
public class HpBar : MonoBehaviour
{
    private const float WIDTH       = 0.85f;
    private const float HEIGHT      = 0.11f;
    private const float HEAD_GAP    = 0.25f;   // world-space gap above sprite top
    private const float FG_INSET    = 0.92f;   // foreground slightly inside bg as a border

    private static Sprite _barSprite;

    private Entity         _entity;
    private SpriteRenderer _fgSR;
    private Transform      _fgT;
    private float          _yOffset;          // computed from entity sprite at Build time

    void Awake()
    {
        _entity = GetComponent<Entity>();
        if (_entity == null) { Destroy(this); return; }
        Build();
    }

    void Start()
    {
        _entity.OnHPChanged += OnHPChanged;
        Refresh();
    }

    void OnDestroy()
    {
        if (_entity != null) _entity.OnHPChanged -= OnHPChanged;
    }

    private void Build()
    {
        if (_barSprite == null) _barSprite = MakeBarSprite();

        // Compute the Y offset so the bar sits just above the entity's sprite top,
        // accounting for sprite pivot and parent scale.
        // bounds.max.y = distance from pivot to sprite TOP in local sprite units.
        // localPosition values get scaled by parent.localScale when computing world pos,
        // so we divide the world-space head gap back by scale to compensate.
        _yOffset = 1.0f;   // fallback if entity has no sprite
        var entitySR = GetComponent<SpriteRenderer>();
        float parentScaleY = Mathf.Max(0.01f, transform.localScale.y);
        if (entitySR != null && entitySR.sprite != null)
            _yOffset = entitySR.sprite.bounds.max.y + (HEAD_GAP / parentScaleY);

        // Find-or-create — prefab template builds the children once; Instantiate clones
        // them; the cloned HpBar's Awake then reuses the existing children rather than
        // duplicating them, while caching its own references to the cloned sprite renderers.
        var bg = transform.Find("HpBar_BG");
        if (bg == null)
        {
            var bgGO = new GameObject("HpBar_BG");
            bgGO.transform.SetParent(transform, false);
            bgGO.transform.localPosition = new Vector3(0f, _yOffset, 0f);
            bgGO.transform.localScale    = new Vector3(WIDTH, HEIGHT, 1f);
            var bgSR = bgGO.AddComponent<SpriteRenderer>();
            bgSR.sprite       = _barSprite;
            bgSR.color        = new Color(0.06f, 0.06f, 0.10f, 0.92f);
            bgSR.sortingOrder = 200;
        }
        else
        {
            // Already exists (cloned) — re-anchor in case sprite/scale differ from the template
            bg.localPosition = new Vector3(0f, _yOffset, 0f);
        }

        var fg = transform.Find("HpBar_FG");
        if (fg == null)
        {
            var fgGO = new GameObject("HpBar_FG");
            fgGO.transform.SetParent(transform, false);
            fgGO.transform.localPosition = new Vector3(0f, _yOffset, 0f);
            fgGO.transform.localScale    = new Vector3(WIDTH * FG_INSET, HEIGHT * 0.72f, 1f);
            var fgSR = fgGO.AddComponent<SpriteRenderer>();
            fgSR.sprite       = _barSprite;
            fgSR.color        = ColorForRatio(1f);
            fgSR.sortingOrder = 201;
            fg = fgGO.transform;
        }
        else
        {
            fg.localPosition = new Vector3(0f, _yOffset, 0f);
        }

        // Cache references — works for both fresh build and reused clones
        _fgT  = fg;
        _fgSR = fg.GetComponent<SpriteRenderer>();
    }

    private void OnHPChanged(int current, int max) => Refresh();

    private void Refresh()
    {
        if (_fgT == null || _entity.maxHP <= 0) return;

        float ratio = Mathf.Clamp01((float)_entity.CurrentHP / _entity.maxHP);
        float w     = WIDTH * FG_INSET;

        // Scale only the X axis. Shift the centre so the LEFT edge stays anchored.
        _fgT.localScale    = new Vector3(w * ratio, HEIGHT * 0.72f, 1f);
        _fgT.localPosition = new Vector3(-w * (1f - ratio) * 0.5f, _yOffset, 0f);
        _fgSR.color        = ColorForRatio(ratio);
    }

    private static Color ColorForRatio(float r) =>
        r > 0.6f ? new Color(0.30f, 0.85f, 0.30f, 1f) :  // green
        r > 0.3f ? new Color(0.95f, 0.75f, 0.10f, 1f) :  // yellow
                   new Color(0.95f, 0.20f, 0.20f, 1f);   // red

    private static Sprite MakeBarSprite()
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
    }
}
