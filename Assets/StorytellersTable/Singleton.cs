
using UnityEngine;
using StorytellersTable.Utility.Log;

/// <summary>
/// Singleton that holds data that's used across files.
/// </summary>
[DefaultExecutionOrder(-100)]   // ensure this is exectued first, so other classes can access this
[DisallowMultipleComponent]
public class Singleton : MonoBehaviour
{
    public static Singleton Instance { get; private set; }

    [SerializeField] public CameraController cameraController;

    [Header("Hex Visual")]
    [SerializeField] public float innerSize = 0f;     // size of the inner hexagon (set to 0 for a normal solid hexagon)
    [SerializeField] public float outerSize = 1f;     // size of the outer hexagon
    [SerializeField] public bool isFlatTopped = false;

    [Header("UI")]
    [SerializeField] public Transform mainCanvas;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        DebugOut.Log(this, "Awake()");

        Instance = this;
    }

    private void OnEnable()
    {
        GameObject obj = new GameObject("Material Loader", typeof(MaterialLoader));
        obj.transform.SetParent(this.transform, true);
    }
}
