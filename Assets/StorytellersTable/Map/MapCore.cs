using StorytellersTable.Map;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StorytellersTable.Core.Data
{
    /// <summary>
    /// Raw tile information.
    /// </summary>
    [Serializable]
    public class TileData
    {
        public HexCoord hexCoord;
        public int mapLayer;
        public string materialId;   // ex. Grass, Water, Snow, Desert, etc..

        /// <summary>
        /// Universal Pointer ID referencing nested lower-tier maps.
        /// If this tile exists on a WorldMap, this field references a StageMap ID.
        /// If on a StageMap, it points to a FloorMap ID.
        /// </summary>
        //public string targetNestedMapId;

        public TileData(HexCoord hexCoord, int mapLayer, string materialId)
        {
            this.hexCoord = hexCoord;
            this.mapLayer = mapLayer;
            this.materialId = materialId;
            //targetNestedMapId = String.Empty;
        }

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
    /// Class to store and manage tile data for a map.
    /// </summary>
    public class MapTileRepresentation
    {
        /// <summary>
        /// Key: layer,
        /// Value: Tile data on that layer.
        /// </summary>
        private readonly Dictionary<int, Dictionary<HexCoord, TileData>> dictTileDatas;
        
        public MapTileRepresentation()
        {
            dictTileDatas = new();
        }

        /// <summary>
        /// Adds a tile to a layer with its given hexcoord contained in <paramref name="data"/>.
        /// </summary>
        /// 
        /// <remarks>
        /// Will override existing entry.
        /// </remarks>
        /// <param name="data"></param>
        public void AddTile(TileData data)
        {
            int layer = data.mapLayer;
            if (!dictTileDatas.ContainsKey(layer))
                dictTileDatas[layer] = new();
            dictTileDatas[layer][data.hexCoord] = data;
        }

        public void AddTiles(UpdateMapInfoPackage package)
        {
            foreach (TileData data in package.info)
                AddTile(data);
        }

        /// <summary>
        /// Adds tile information from <paramref name="other"/>.
        /// </summary>
        /// 
        /// <remarks>
        /// If <paramref name="replaceExistingEntry"/> is `true`, existing entry's will be overridden by the ones in <paramref name="other"/>.
        /// </remarks>
        /// <param name="other"></param>
        /// <param name="replaceExistingEntry"></param>
        public void AddFromMapTileRepresentation(MapTileRepresentation other, bool replaceExistingEntry = false)
        {
            foreach (Dictionary<HexCoord, TileData> dict in other.GetTileRepresentation().Values)
            {
                // in this for-loop, all the TileData's are guranteed to have the same `mapLayer` value
                foreach ((HexCoord hexCoord, TileData data) in dict)
                {
                    int layer = data.mapLayer;
                    // check if it has this layer
                    if (!dictTileDatas.TryGetValue(layer, out var _))
                        dictTileDatas[layer] = new();

                    // A TileData already exists with the given entry: [layer][hexCoord], skip it (if `replaceExistingEntry` is also false)
                    if (dictTileDatas[layer].TryGetValue(hexCoord, out TileData _))
                    {
                        if (!replaceExistingEntry)
                            continue;

                        // remove existing entry
                        dictTileDatas[layer].Remove(hexCoord);
                    }

                    // add new entry
                    dictTileDatas[layer].Add(hexCoord, data);
                }
            }
        }

        public void UpdateTile(TileData data)
        {
            int layer = data.mapLayer;
            if (!EntryExists(data))
                return;
            dictTileDatas[layer][data.hexCoord] = data;
        }

        public void UpdateTiles(UpdateMapInfoPackage package)
        {
            foreach (TileData data in package.info)
                UpdateTile(data);
        }

        /// <summary>
        /// Try's to remove tile data at layer stored in <paramref name="data"/>. Only removes entry's that exist.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>        
        public bool TryRemove(TileData data)
        {
            int layer = data.mapLayer;
            if (!EntryExists(data))
                return false;
            dictTileDatas[layer].Remove(data.hexCoord);
            return true;
        }

        public void TryRemoveMultiple(UpdateMapInfoPackage package)
        {
            foreach (TileData data in package.info)
                TryRemove(data);
        }

        public bool EntryExists(TileData data)
        {
            return EntryExists(data.mapLayer, data.hexCoord);
        }

        public bool EntryExists(int layer, HexCoord hexCoord)
        {
            if (!dictTileDatas.ContainsKey(layer))
                return false;
            if (!dictTileDatas[layer].ContainsKey(hexCoord))
                return false;
            return true;
        }

        public Dictionary<int, Dictionary<HexCoord, TileData>> GetTileRepresentation()
        {
            return dictTileDatas;
        }

        public void Clear()
        {
            dictTileDatas.Clear();
        }

        /// <summary>
        /// Return's the number of layerS in the map.
        /// </summary>
        /// <returns></returns>
        public int LayerCount()
        {
            return dictTileDatas.Count;
        }

        /// <summary>
        /// Return's the map size for a given layer.
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        public int LayerSize(int layer)
        {
            return dictTileDatas[layer].Count;
        }

        /// <summary>
        /// Return's the total number of tiles on the map.
        /// </summary>
        /// <returns></returns>
        public int TotalSize()
        {
            int size = 0;
            foreach ((int layer, Dictionary<HexCoord, TileData> value) in dictTileDatas)
            {
                size += value.Count;
            }
            return size;
        }
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
