
using StorytellersTable.Core.Data;
using StorytellersTable.Utility.Log;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StorytellersTable
{
    public static class HexMath
    {
        /// <summary>
        /// Get's adjacent HexCoords based on <paramref name="center"/> and <paramref name="checkExists"/>.
        /// </summary>
        /// 
        /// <remarks>
        /// If <paramref name="checkExists"/> is <c>true</c>, it will only include HexCoords that exist in <paramref name="mapData"/>, 
        /// ie tile data that exists at the HexCoord.
        /// </remarks>
        /// <param name="mapData"></param>
        /// <param name="center"></param>
        /// <param name="checkExists"></param>
        /// <returns></returns>
        //public static List<HexCoord> GetAdjacentHexCoords(MapData mapData, HexCoord center, bool checkExists = false)
        //{
        //    // We can compute in O(1) time the adjacent hex tiles instead having to store them
        //    List<HexCoord> result = new List<HexCoord>();

        //    foreach (HexCoord offset in HexCoord.ADJACENT_TILE_OFFSETS)
        //    {
        //        // don't check if the tile exists on the map
        //        if (!checkExists)
        //            result.Add(center + offset);
        //        // check if the tile exists (not null) on the map
        //        else if (checkExists && mapData.tileDatas.TryGetValue(center + offset, out TileData data)) // need to consider if they want to `fall through empty cells`
        //            result.Add(center + offset);
        //    }

        //    return result;
        //}

        #region Ring & Area & Line coord calculations

        /// <summary>
        /// Calculates all hex coordinates that are exactly <paramref name="radius"/> steps away from the <paramref name="center"/>.
        /// </summary>
        /// 
        /// <remarks>
        /// The algorithm starts at an initial corner tile by scaling a starting direction by the radius.
        /// It then walks along the 6 sides of the hexagon. Each side consists of exactly <paramref name="radius"/> steps 
        /// taken in sequential counter-clockwise direction utilizing the neighbor offsets (HexCoord.ADJACENT_TILE_OFFSETS).
        /// </remarks>
        /// <param name="center">The central axial coordinate.</param>
        /// <param name="radius">The exact distance from the center tile (must be greater than 0).</param>
        /// <param name="results">A pre-allocated list to store the resulting coordinates.</param>
        public static void GetHexRing(HexCoord center, int radius, HashSet<HexCoord> results)
        {
            if (radius == 0)
                return;

            // top right tile relative to center based on radius
            HexCoord currTile = center + (HexCoord.ADJACENT_TILE_OFFSETS[1] * radius);

            // walk along the 6 sides of a hexagon, counter clockwise
            for (int side = 0; side < 6; side++)
            {
                // Get the direction to walk based on the side we are on
                HexCoord walkDirection = HexCoord.ADJACENT_TILE_OFFSETS[5 - side];
                /*
                 * Based on a flat-top orintation,
                 * side 0: is top of the hexagon,
                 * side 1: is the top-left diagonal,
                 * side 2: is the bottom-left diagonal,
                 * side 3: is the bottom,
                 * side 4: is the bottom-right diagonal,
                 * side 5: is the top-right diagonal.
                */

                // Walk along the side `n` times, the number of tiles along the side to walk will be `radius`
                // note: the top right-most tile will be added at side 6 iteration.
                for (int n = 0; n < radius; n++)
                {
                    currTile += walkDirection;
                    results.Add(currTile);
                }
            }
        }

        /// <summary>
        /// Calculates all hex coordinates from the center point outward to exactly <paramref name="radius"/> steps away from the <paramref name="center"/>.
        /// </summary>
        /// 
        /// <remarks>
        /// Ex. If the <paramref name="radius"/> is 2, then it will get the hex rings for radius 1 and 2, and include them to the <paramref name="results"/>.
        /// </remarks>
        /// <param name="center">The central axial coordinate.</param>
        /// <param name="radius">The exact distance from the center tile (must be greater than 0).</param>
        /// <param name="results">A pre-allocated list to store the resulting coordinates.</param>
        public static void GetHexRingArea(HexCoord center, int radius, HashSet<HexCoord> results)
        {
            for (int currRadius = 1; currRadius <= radius; currRadius++)
            {
                GetHexRing(center, currRadius, results);
            }
        }

        /// <summary>
        /// Calculates all hex coordinates between <paramref name="start"/> and <paramref name="end"/>, based on axial coordinates q, r.
        /// </summary>
        /// <param name="start">The starting axial coordinate. Must be different from <paramref name="end"/>.</param>
        /// <param name="end">The ending axial coordinate. Must be different from <paramref name="start"/>.</param>
        /// <param name="results">A pre-allocated list to store the resulting coordinates.</param>
        public static void GetAreaAxial(HexCoord start, HexCoord end, HashSet<HexCoord> results)
        {
            if (start == end)
                return;

            // define boundry of the area, regardless of where start and end are located from each other
            int minQ = Mathf.Min(start.q, end.q);
            int maxQ = Mathf.Max(start.q, end.q);
            int minR = Mathf.Min(start.r, end.r);
            int maxR = Mathf.Max(start.r, end.r);

            for (int q = minQ; q <= maxQ; q++)
            {
                for (int r = minR; r <= maxR; r++)
                    results.Add(new HexCoord(q, r));
            }
        }

        public static void GetLine(HexCoord start, HexCoord end, HashSet<HexCoord> results)
        {
            if (start == end)
                return;


        }

        #endregion

        #region World position <-> Hex coordinate conversions

        /// <summary>
        /// Computes the exact 3D world position from the hex coordinate using structural basis vector matrix transformations.
        /// This removes all floating point tracking gaps and anchors the origin natively at (0,0,0).
        /// </summary>
        public static Vector3 GetPositionFromAxial(HexCoord coord)
        {
            float xPosition = 0f;
            float zPosition = 0f;
            float size = Singleton.Instance.outerSize;

            if (!Singleton.Instance.isFlatTopped)
            {
                // Pointy-Topped Basis Matrix 
                xPosition = size * (Mathf.Sqrt(3f) * coord.q + Mathf.Sqrt(3f) / 2f * coord.r);
                zPosition = size * (3f / 2f * coord.r);
            }
            else
            {
                // Flat-Topped Basis Matrix
                xPosition = size * (3f / 2f * coord.q);
                zPosition = size * (Mathf.Sqrt(3f) / 2f * coord.q + Mathf.Sqrt(3f) * coord.r);
            }

            // Inverting the Z axis to maintain your layout structure starting from top-left progression
            return new Vector3(xPosition, 0f, -zPosition);
        }

        /// <summary>
        /// Converts a 3D world position (using X and Y) into a discrete integer Axial HexCoord.
        /// </summary>
        /// <param name="worldPos"></param>
        /// <returns></returns>
        public static HexCoord WorldToAxial(Vector3 worldPos)
        {
            float size = Singleton.Instance.outerSize;
            float fracQ, fracR;
            float worldX = worldPos.x;
            float worldZ = -worldPos.z; // apply layout space restoration up front

            if (!Singleton.Instance.isFlatTopped)
            {
                // Pointy-top matrix inversion transformation
                fracQ = (Mathf.Sqrt(3f) / 3f * worldX - 1f / 3f * worldZ) / size;
                fracR = (2f / 3f * worldZ) / size;
            }
            else
            {
                // Flat-top matrix inversion transformation
                fracQ = (2f / 3f * worldX) / size;
                fracR = (-1f / 3f * worldX + Mathf.Sqrt(3f) / 3f * worldZ) / size;
            }

            // Convert to 3D cube coordinates to do robust rounding (ensuring q + r + s = 0)
            float fracS = -fracQ - fracR;

            int q = Mathf.RoundToInt(fracQ);
            int r = Mathf.RoundToInt(fracR);
            int s = Mathf.RoundToInt(fracS);

            // Calculate the rounding deltas
            float qDiff = Mathf.Abs(q - fracQ);
            float rDiff = Mathf.Abs(r - fracR);
            float sDiff = Mathf.Abs(s - fracS);

            // Re-adjust the axis with the largest rounding error to satisfy q + r + s = 0
            if (qDiff > rDiff && qDiff > sDiff)
            {
                q = -r - s;
            }
            else if (rDiff > sDiff)
            {
                r = -q - s;
            }
            // (If sDiff is largest, no adjustments to q or r are required)

            return new HexCoord(q, r);
        }

        #endregion
    }

}
