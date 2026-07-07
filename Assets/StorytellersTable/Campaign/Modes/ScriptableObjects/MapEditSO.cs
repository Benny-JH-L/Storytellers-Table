using System;
using UnityEngine;

[CreateAssetMenu(fileName = "MapEditSO", menuName = "Scriptable Objects/MapEditSO")]
[Serializable]
public class MapEditSO : ScriptableObject
{
    [Header("Radial editing")]
    [Range(1, 10)]
    [SerializeField] public int radius = 3;
}

