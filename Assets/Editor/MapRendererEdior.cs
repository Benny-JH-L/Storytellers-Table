
using StorytellersTable.Map;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Adds aditional content to the Unity Inspector for MapBase.cs scripts and derivatives.
/// </summary>
[CustomEditor(typeof(MapRenderer), true)] // true means derrived classes will see this too
public class MapRendererEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        //DrawDefaultInspector(); // draws every thing in the base editor

        GUILayout.Space(10);

        MapRenderer mapRenderer = (MapRenderer)target;

        //if (GUILayout.Button("Rebuild Map"))
        //{

        //    Undo.RegisterFullObjectHierarchyUndo(
        //        mapRenderer.gameObject,
        //        "Rebuild Map"
        //    );  // save current map state to the undo stack

        //    mapRenderer.RebuildMap();

        //    EditorUtility.SetDirty(mapRenderer);
        //}

        if (GUILayout.Button("Re Draw Hex Tile Mesh"))
        {
            mapRenderer.activeMap.graph.ReDrawTileMesh();
        }
    }
}
