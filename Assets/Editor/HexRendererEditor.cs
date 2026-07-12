
using UnityEditor;
using UnityEngine;
using StorytellersTable.Renderer;

[CustomEditor(typeof(HexRenderer), true)]
public class HexRendererEditor : Editor
{
    // Cached Shader Property IDs for high-performance stringless lookups
    private static readonly int IsHighlightedProp = Shader.PropertyToID("_IsHighlighted");
    private static readonly int IsSelectedProp = Shader.PropertyToID("_IsSelected");

    // Property block reused across all hex instances
    private static MaterialPropertyBlock _materialPropertyBlock;

    public void OnEnable()
    {
        _materialPropertyBlock = new MaterialPropertyBlock();
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        HexRenderer hexRenderer = (HexRenderer)target;
        hexRenderer.GetMeshRenderer().GetPropertyBlock(_materialPropertyBlock);
        float highlightVal = _materialPropertyBlock.GetFloat(IsHighlightedProp);
        float selectedVal = _materialPropertyBlock.GetFloat(IsSelectedProp);

        if (GUILayout.Button("Redraw mesh"))
            hexRenderer.DrawMesh();

        if (GUILayout.Button("Toggle Highlight visual"))
        {
            hexRenderer.ToggleHighlight();
        }
        GUILayout.Label("Highlight visual " + (highlightVal > 0 ? "ON" : "OFF"));

        if (GUILayout.Button("Toggle Select visual"))
        {
            hexRenderer.ToggleSelectedVisual();
        }
        GUILayout.Label("Selected visual " + (selectedVal > 0 ? "ON" : "OFF"));
    }
}
