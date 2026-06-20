
using System;
using System.Collections.Generic;
using UnityEngine;

//public enum TileType
//{
//    Grass, Water, Snow, Desert // etc..
//};

public struct HexCoord : IEquatable<HexCoord>
{
    public static readonly List<HexCoord> ADJACENT_TILE_OFFSETS = new List<HexCoord>(){
        new HexCoord(0, -1),    // top left
        new HexCoord(1, -1),    // top right
        new HexCoord(1, 0),     // right
        new HexCoord(0, 1),     // bottom right
        new HexCoord(-1, 1),    // bottom left
        new HexCoord(-1, 0),    // left
                                
    };
    // ^ comments based on pointy-top representation, but logic works for flat-top tiles too

    public int q, r;
    public HexCoord(int q_, int r_)
    { 
        q = q_;
        r = r_;
    }

    public static HexCoord operator +(HexCoord h1, HexCoord h2)
    {
        return new HexCoord(h1.q + h2.q, h1.r + h2.r);
    }

    public static HexCoord operator -(HexCoord h1, HexCoord h2)
    {
        return new HexCoord(h1.q - h2.q, h1.r - h2.r);
    }

    public static bool operator ==(HexCoord left, HexCoord right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(HexCoord left, HexCoord right)
    {
        return !(left == right);
    }

    ///// <summary>
    ///// Convert from axial Hex coordinate to cube coordinate
    ///// </summary>
    ///// <param name="hexCoord"></param>
    ///// <returns></returns>
    public static CubeCoord ToCube(HexCoord hexCoord) => new CubeCoord(hexCoord.q, hexCoord.r);

    public override int GetHashCode()
    {
        return HashCode.Combine(q, r);
    }

    public override string ToString()
    {
        return $"({q}, {r})";
    }

    public override bool Equals(object obj)
    {
        return (obj is HexCoord) && Equals((HexCoord)obj);
    }

    public bool Equals(HexCoord other)
    {
        return q == other.q && r == other.r;
    }
};

public struct CubeCoord
{
    public int q, r, s;
    public CubeCoord(int q_, int r_)
    {
        q = q_;
        r = r_;
        s = -q - r;
    }

    /// <summary>
    /// Convert from cube coordinate to axial hex coordinate
    /// </summary>
    /// <param name="cubeCoord"></param>
    /// <returns></returns>
    public static HexCoord ToAxial(CubeCoord cubeCoord) => new HexCoord(cubeCoord.q, cubeCoord.r);

    public override string ToString()
    {
        return $"({q}, {r}), {s}";
    }
};

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
public abstract class TileBase
{    
    //HexCoord hexCoord; // can be computed based on world position
    int elevation;

    //TileType tileType;
    //public Material material;

    public TileBase()
    {
        // tileType determines material obtained
    }
}


[Serializable]
public class WorldTile : TileBase
{
    public string stageMapID;   // own's a stage map
    // data...

    public WorldTile()
    {

    }
}


[Serializable]
public class StageTile : TileBase
{
    public string floorMapID;   // own's a floor map
    // data...

    public StageTile()
    {

    }
}


[Serializable]
public class FloorTile : TileBase
{
    // data...
    
    public FloorTile()
    {

    }
}