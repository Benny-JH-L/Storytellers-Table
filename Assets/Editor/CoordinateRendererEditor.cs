
using StorytellersTable.Map;
using UnityEditor;
using UnityEngine;

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


