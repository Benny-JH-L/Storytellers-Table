using System;
using System.Collections.Generic;
using UnityEngine;

namespace StorytellersTable.Core.Data
{
    /// <summary>
    /// Categorizes the hierarchical tiered nesting depth of a map. 
    /// World is the heighest tier, Stage middle tier, and Floor is the lowest tier.
    /// </summary>
    public enum MapTier
    {
        World,
        Stage,
        Floor
    }

    /// <summary>
    /// Base class representing structural map characteristics and coordinate tracking.
    /// </summary>
    [Serializable]
    public abstract class MapData
    {
        public string mapId;
        public string mapName;
        public MapTier tier;
        //public Vector2Int mapSize;   // map size is dynamic and can be variable width and size: based on player map edits

        public Graph graph;

        public MapData()
        {
            graph = new Graph();
        }

        /// <summary>
        /// Map hex coords to pure data elements
        /// </summary>
        public Dictionary<HexCoord, TileData> tileDatas = new Dictionary<HexCoord, TileData>();

        /// <summary>
        /// Clears and destroys all map tiles on this map
        /// </summary>
        public void ClearTiles() => graph.Clear();
    }

    [Serializable]
    public class WorldMap : MapData
    {
        // POI labels, regional labels, etc.

        public WorldMap() : base()
        {
            tier = MapTier.World;
        }
    }

    [Serializable]
    public class StageMap : MapData
    {
        // POI labels, regional labels, etc.

        public StageMap() : base()
        {
            tier = MapTier.Stage;
        }
    }

    [Serializable]
    public class FloorMap : MapData
    {
        public FloorMap() : base()
        {
            tier = MapTier.Floor;
        }
    }
}
