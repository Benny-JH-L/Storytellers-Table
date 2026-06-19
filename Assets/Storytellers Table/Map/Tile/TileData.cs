
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

//public enum TileType
//{
//    Grass, Water, Snow, Desert // etc..
//};

public struct HexCoord
{
    public int q, r;
    public HexCoord(int q_, int r_)
    { 
        q = q_;
        r = r_;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(q, r);
    }

    public override string ToString()
    {
        return $"({q}, {r})";
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
    public override string ToString()
    {
        return $"({q}, {r}), {s}";
    }
};


[Serializable]
public abstract class TileData
{
    HexCoord hexCoord;
    int elevation;

    //TileType tileType;
    //public Material material;

    public TileData ()
    {
        // tileType determines material obtained
    }
}


[Serializable]
public class WorldTile : TileData
{
    public string stageMapID;   // own's a stage map
    // data...
}


[Serializable]
public class StageTile : TileData
{
    public string floorMapID;   // own's a floor map
    // data...
}


[Serializable]
public class FloorTile : TileData
{
    // data...
}