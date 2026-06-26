
using System;
using System.Collections.Generic;
using UnityEngine;

//public enum TileType
//{
//    Grass, Water, Snow, Desert // etc..
//};

[RequireComponent(typeof(HexRenderer))]
public class TileComponent : MonoBehaviour
{
    [SerializeReference] private TileBase _tileData;    // holds data for WorldTile, StageTile, FloorTile

    public HexCoord HexCoord { get; private set; }          // getter, setter, and variable for `HexCoord`
    public HexRenderer HexRenderer { get; private set; }    // getter, setter, and variable for `HexRenderer`

    private void Awake()
    {
        HexRenderer = GetComponent<HexRenderer>();
    }

    /// <summary>
    /// Initializes the visual tile component with its data context and coordinates.
    /// </summary>
    public void Setup(HexCoord hexCoord, TileBase tileData)
    {
        HexCoord = hexCoord;
        _tileData = tileData;
    }

    /// <summary>
    /// Type-safe getter to fetch the data model casted to its explicit type.
    /// Usage: var worldData = tileComp.GetData<WorldTile>();
    /// </summary>
    public T GetData<T>() where T : TileBase
    {
        return _tileData as T;
    }
}

[Serializable]
public class TileBase
{    
    HexCoord hexCoord;
    int elevation;
    
    //TileType tileType;
    //public Material material;

    public TileBase(HexCoord hexCoord)
    {
        this.hexCoord = hexCoord;
        // tileType determines material obtained
    }

    // HEAR ME OUT, TileBase contains all the data for World Tile, Stage Tile, and Floor tile, but only select stuff is shown based on map!
    // (makes the map editor logic and UI easier to make!
}


//[Serializable]
//public class WorldTile : TileBase
//{
//    public string stageMapID;   // own's a stage map
//    // data...

//    public WorldTile()
//    {

//    }
//}


//[Serializable]
//public class StageTile : TileBase
//{
//    public string floorMapID;   // own's a floor map
//    // data...

//    public StageTile()
//    {

//    }
//}


//[Serializable]
//public class FloorTile : TileBase
//{
//    // data...
    
//    public FloorTile()
//    {

//    }
//}