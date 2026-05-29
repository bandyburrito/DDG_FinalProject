using UnityEngine;

/// <summary>
/// Procedural impact-burst — small radial spray of fading particles.
/// Called from Entity.TakeDamage to give visual feedback on every hit.
/// No prefab needed — sprites are generated at runtime.
/// </summary>
public static class HitBurst
{
    private static Sprite _particleSprite;

    public static void SpawnAt(Vector3 worldPos, Color tint, int count = 8)
    {
        if (_particleSprite == null) _particleSprite = MakeSprite();

        var container = new GameObject("HitBurst");
        container.transform.position = worldPos;

        for (int i = 0; i < count; i++)
        {
            // Even radial distribution with a small jitter so it doesn't look mechanical
            float angle = (i / (float)count) * 360f + Random.Range(-12f, 12f);
            float speed = Random.Range(2.4f, 4.5f);
            float life  = Random.Range(0.28f, 0.5f);
            float size  = Random.Range(0.10f, 0.18f);

            var p = new GameObject($"Particle_{i}");
            p.transform.SetParent(container.transform, false);
            p.transform.localScale = Vector3.one * size;

            var sr = p.AddComponent<SpriteRenderer>();
            sr.sprite       = _particleSprite;
            sr.color        = tint;
            sr.sortingOrder = 250;

            // Squash Y by 0.5 so the spray feels grounded in iso projection
            Vector2 dir = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad) * 0.55f
            );

            var mover = p.AddComponent<HitParticle>();
            mover.Init(dir * speed, life);
        }

        Object.Destroy(container, 0.7f);
    }

    private static Sprite MakeSprite()
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
    }
}

/// <summary>
/// Single particle: moves with friction + gravity, fades and shrinks to zero, self-destructs.
/// </summary>
public class HitParticle : MonoBehaviour
{
    private Vector2        _velocity;
    private float          _life;
    private float          _elapsed;
    private SpriteRenderer _sr;
    private Color          _startColor;
    private Vector3        _startScale;

    public void Init(Vector2 velocity, float life)
    {
        _velocity = velocity;
        _life     = life;
    }

    void Awake()
    {
        _sr         = GetComponent<SpriteRenderer>();
        _startScale = transform.localScale;
    }

    void Start()
    {
        if (_sr != null) _startColor = _sr.color;
    }

    void Update()
    {
        _elapsed += Time.deltaTime;
        if (_elapsed >= _life) { Destroy(gameObject); return; }

        // Physics-lite: friction + small downward pull
        transform.position += new Vector3(_velocity.x, _velocity.y, 0f) * Time.deltaTime;
        _velocity   *= 0.90f;
        _velocity.y -= 4.5f * Time.deltaTime;

        // Fade + shrink over lifetime
        float t = _elapsed / _life;
        if (_sr != null)
        {
            var c = _startColor;
            c.a   = 1f - t;
            _sr.color = c;
        }
        transform.localScale = _startScale * (1f - t * 0.45f);
    }
}
