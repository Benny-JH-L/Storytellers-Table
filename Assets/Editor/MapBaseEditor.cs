
//using UnityEditor;
//using UnityEngine;

///// <summary>
///// Adds aditional content to the Unity Inspector for MapBase.cs scripts and derivatives.
///// </summary>
//[CustomEditor(typeof(MapData), true)] // true means derrived classes will see this too
//public class MapBaseEditor : Editor
//{
//    public override void OnInspectorGUI()
//    {
//        base.OnInspectorGUI();
//        //DrawDefaultInspector(); // draws every thing in the base editor

//        GUILayout.Space(10);
        
//        MapData map = (MapData)target;

//        if (GUILayout.Button("Rebuild Map"))
//        {
            
//            Undo.RegisterFullObjectHierarchyUndo(
//                map.gameObject,
//                "Rebuild Map"
//            );  // save current map state to the undo stack

//            map.RebuildMap();

//            EditorUtility.SetDirty(map);
//        }

//        if (GUILayout.Button("Re Draw Hex Tile Mesh"))
//        {
//            map.ReDrawTileMesh();
//        }
//    }
//}
