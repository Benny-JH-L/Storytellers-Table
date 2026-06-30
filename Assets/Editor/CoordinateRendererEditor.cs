
using UnityEditor;
using UnityEngine;
using StorytellersTable.Renderer;

[CustomEditor((typeof(CoordinatesRenderer)), true)]
public class CoordinateRendererEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        CoordinatesRenderer coordinatesRenderer = (CoordinatesRenderer)target;

        if (GUILayout.Button("Toggle Labels"))
            coordinatesRenderer.ToggleLabels();
    }
}


