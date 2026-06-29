
using StorytellersTable.Map;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Adds aditional content to the Unity Inspector for MapManager.cs scripts and derivatives.
/// </summary>
[CustomEditor(typeof(MapManager), true)] // true means derrived classes will see this too
public class MapManagerEditor : Editor
{
    public string switchToMapId = string.Empty;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        //DrawDefaultInspector(); // draws every thing in the base editor

        GUILayout.Space(10);
        
        MapManager mapManager = (MapManager)target;

        if (GUILayout.Button("Rebuild Map"))
        {
            
            Undo.RegisterFullObjectHierarchyUndo(
                mapManager.gameObject,
                "Rebuild Map"
            );  // save current map state to the undo stack

            mapManager.RebuildMap();

            EditorUtility.SetDirty(mapManager);
        }

        if (GUILayout.Button("Re Draw Hex Tile Mesh"))
        {
            mapManager.ReDrawTileMesh();
        }

        if (GUILayout.Button("Clear Tiles"))
        {
            mapManager.ClearTiles();
        }

        GUILayout.Label("Switch to map with id: ");
        switchToMapId = GUILayout.TextField(switchToMapId);
        if (GUILayout.Button("Switch Map"))
        {
            mapManager.SwitchActiveMap(switchToMapId);
        }
    }
}
