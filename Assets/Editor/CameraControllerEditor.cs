
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CameraController), true)]
public class CameraControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.Space(10);

        CameraController cameraController = (CameraController)target;
        if (GUILayout.Button("Toggle Psuedo Orthographic Projection"))
        {
            cameraController.TogglePeseudoOrthographic();
        }
    }
}