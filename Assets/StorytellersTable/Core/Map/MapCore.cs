using System;
using UnityEngine;

namespace Assets.StorytellersTable.Core.Map
{
    /// <summary>
    /// Raw tile information.
    /// </summary>
    [Serializable]
    public class TileData
    {
        public HexCoord hexCoord;
        public Layer mapLayer;
        public string materialId;   // ex. Grass, Water, Snow, Desert, etc..

        /// <summary>
        /// Universal Pointer ID referencing nested lower-tier maps.
        /// If this tile exists on a WorldMap, this field references a StageMap ID.
        /// If on a StageMap, it points to a FloorMap ID.
        /// </summary>
        //public string targetNestedMapId;

        public TileData(HexCoord hexCoord, Layer mapLayer, string materialId)
        {
            this.hexCoord = hexCoord;
            this.mapLayer = mapLayer;
            this.materialId = materialId;
            //targetNestedMapId = String.Empty;
        }

        public TileData(HexCoord hexCoord, int mapLayer, string materialId) : this(hexCoord, new Layer(mapLayer), materialId)
        { }

        public Material GetMaterial()
        {
            return MaterialLoader.instance.GetMaterial(materialId);
        }

        public override string ToString()
        {
            //return $"HexCoord{hexCoord.ToString()} | layer[{layer}] | tileType[{tileTypeId} | nestedMapTarget[{targetNestedMapId}]]";
            return $"HexCoord{hexCoord.ToString()} | layer[{mapLayer}] | tileType[{materialId}";
        }

        // HEAR ME OUT, TileBase contains all the data for World Tile, Stage Tile, and Floor tile, but only select stuff is shown based on map!
        // (makes the map editor logic and UI easier to make!
    }

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
        [SerializeField] public string mapId;
        [SerializeField] public string mapName;
        [SerializeField] public MapTier tier;

        /// <summary>
        /// Map hex coords to pure data elements.
        /// </summary>
        //public Dictionary<HexCoord, TileData> tileDatas = new Dictionary<HexCoord, TileData>();
        public MapTileRepresentation mapTileData;

        public MapData()
        {
            mapTileData = new();
        }
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
