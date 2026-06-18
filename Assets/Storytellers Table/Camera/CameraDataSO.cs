using UnityEngine;

[CreateAssetMenu(fileName = "CameraDataSO", menuName = "Scriptable Objects/CameraDataSO")]
[System.Serializable]
public class CameraData : ScriptableObject
{
    public float zoomSpeed = 200f;

    // map
    [Header("Map")]
    [Range(0.25f, 4)]
    public float mapMoveSensitivity = 2f;
    [Range(50, 300)]    // tentative
    public float mapZoomInOutSensitivity = 200f;

    // orbit camera
    [Header("Orbit")]
    public float initXRotatation = 0f;      // theta
    public float initYRotatation = 45f;     // phi
    public float initOrbitDistance = 30f;
    public float orbitSens = 30f;
    public float orbitDistanceZoomSens = 2f;
    public float orbitMinDist = 10f;
    public float orbitMaxDist = 100f;
    public float orbitMinVerticalClamp = -20f;
    public float orbitMaxVerticalClamp = 90f;

    // perspective
    [Header("Perspective")]
    public float minZoom = 30f;
    public float maxZoom = 90f;

    // orthographic
    [Header("Orthographic")]
    public float minOrthoSize = 2f;
    public float maxOrthoSize = 30f;

    // maybe a free cam?
    [Header("Free cam?")]
    public float minVerticalClamp = -89f;
    public float maxVerticalClamp = 89f;
}
