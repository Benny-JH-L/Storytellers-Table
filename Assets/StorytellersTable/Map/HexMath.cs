
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
        public static List<HexCoord> GetAdjacentHexCoords(MapData mapData, HexCoord center, bool checkExists = false)
        {
            // We can compute in O(1) time the adjacent hex tiles instead having to store them
            List<HexCoord> result = new List<HexCoord>();

            foreach (HexCoord offset in HexCoord.ADJACENT_TILE_OFFSETS)
            {
                // don't check if the tile exists on the map
                if (!checkExists)
                    result.Add(center + offset);
                // check if the tile exists (not null) on the map
                else if (checkExists && mapData.tileDatas.TryGetValue(center + offset, out TileData data))
                    result.Add(center + offset);
            }

            return result;
        }

        #region Ring & Area coord getting

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
        public static void GetHexRing(HexCoord center, int radius, List<HexCoord> results)
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
        public static void GetHexRingArea(HexCoord center, int radius, List<HexCoord> results)
        {
            for (int currRadius = 1; currRadius < radius; currRadius++)
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
        public static void GetAreaAxial(HexCoord start, HexCoord end, List<HexCoord> results)
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

        #endregion

    }

}
