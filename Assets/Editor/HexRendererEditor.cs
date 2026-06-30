
using UnityEditor;
using UnityEngine;
using StorytellersTable.Renderer;

[CustomEditor(typeof(HexRenderer), true)]
public class HexRendererEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        HexRenderer hexRenderer = (HexRenderer)target;

        if (GUILayout.Button("Redraw mesh"))
            hexRenderer.DrawMesh();
    }
}
