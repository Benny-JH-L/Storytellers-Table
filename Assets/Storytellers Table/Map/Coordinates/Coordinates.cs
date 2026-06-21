
using System;
using System.Collections.Generic;

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
