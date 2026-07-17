
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TileEditContainer), true)]
public class TileEditModeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        TileEditContainer tileEditMode = (TileEditContainer)target;

        if (GUILayout.Button("Confirm Height"))
        {
            tileEditMode.ConfirmHeightEdit();
        }
        if (GUILayout.Button("Confirm Material"))
        {
            tileEditMode.ConfirmMaterialEdit();
        }
        if (GUILayout.Button("Confirm All Edits"))
        {
            tileEditMode.ConfirmAllEdits();
        }
    }
}
