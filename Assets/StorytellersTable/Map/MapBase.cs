using StorytellersTable.Core.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class MapData2 : MonoBehaviour
{
    [Header("Grid Settings")]
    public Vector2Int mapSize;

    [Header("Other")]
    public string mapID;
    [SerializeField] public Graph graph;

    // strictly for the Unity inspector
    [Header("OnValidate()")]
    [SerializeField] private bool toggleOnValidate;

    // shared stuff: convert mouse pixel pos -> hex, etc...
    public abstract void Setup();
    //public abstract TileBase CreateTileDataInstance(HexCoord hexCoord);

    private void Awake()
    {
        Setup();
        toggleOnValidate = false;
    }

    private void OnEnable()
    {
        //StorytellersTable.Campaign.Modes.MapEditMode.LayoutMap(this, mapSize);
    }

    private void OnValidate()
    {
        if (toggleOnValidate && Application.isPlaying)
        {
            // what ever you want...
        }
    }

    /// <summary>
    /// Rebuilds the map
    /// </summary>
    [ContextMenu("Rebuild Map")] // In the Unity inspector, right click the map script, and select this option
    public void RebuildMap()
    {
        Debug.Log($"Re building map of size q={mapSize.x}, r={mapSize.y}...");
        //StorytellersTable.Campaign.Modes.MapEditMode.LayoutMap(this, mapSize);
    }

    [ContextMenu("Re Draw Hex Tile Mesh")] // In the Unity inspector, right click the map script, and select this 
    public void ReDrawTileMesh()
    {
        Debug.Log("Re drawing tile mesh...");
        graph.ReDrawTileMesh();
    }

    /// <summary>
    /// Clears and destroys all map tiles on this map
    /// </summary>
    public void ClearTiles()
    {
        graph.Clear();
    }

}
