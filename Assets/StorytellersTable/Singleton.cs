
using UnityEngine;

/// <summary>
/// Singleton that holds data that's used across files.
/// </summary>
[DisallowMultipleComponent]
public class Singleton : MonoBehaviour
{
    public static Singleton Instance { get; private set; }

    [Header("Tile")]
    public Material defaultTileMaterial;
    public Material ghostTileMaterial;      // for now it will be one material, can change it in the future to reflect the specific tile type (ie snow, grass, etc.)

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
}
