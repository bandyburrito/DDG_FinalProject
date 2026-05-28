using UnityEngine;

/// <summary>Stub — all visible UI is drawn by PlaceholderUI via OnGUI.</summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
}
