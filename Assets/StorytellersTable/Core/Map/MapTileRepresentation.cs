
using StorytellersTable.Map;
using System.Collections.Generic;

namespace Assets.StorytellersTable.Core.Map
{
    /// <summary>
    /// Class to store and manage tile data for a map.
    /// </summary>
    public class MapTileRepresentation
    {
        /// <summary>
        /// Key: layer,
        /// Value: Tile data on that layer.
        /// </summary>
        private readonly Dictionary<Layer, Dictionary<HexCoord, TileData>> dictTileDatas;

        public MapTileRepresentation()
        {
            dictTileDatas = new();
        }

        /// <summary>
        /// Return's a list of <paramref name="datas"/> with the HexCoord <paramref name="hexCoord"/>, 
        /// from a range of layers, between [<paramref name="max"/>, <paramref name="min"/>].
        /// </summary>
        /// 
        /// <remarks>
        /// <paramref name="datas"/> will be sorted in descending order based on its layer.
        /// </remarks>
        /// <param name="hexCoord"></param>
        /// <param name="datas"></param>
        public void GetTileDataStack(HexCoord hexCoord, out List<TileData> datas, int max = int.MaxValue, int min = int.MinValue)
        {
            datas = new();

            // TODO: need something to set the search range if the `max` and `min` aren't set; use the existing layers in the dict to set the search range

            for (int layerVal = min; layerVal <= max; layerVal++)
            {
                Layer layer = new (layerVal);
                // try to get the layer's tile data
                if (dictTileDatas.TryGetValue(layer, out Dictionary<HexCoord, TileData> dict))
                {
                    // get the tile data if it exists
                    if (dict.TryGetValue(hexCoord, out TileData tileData))
                        datas.Add(tileData);
                }
            }
            datas.Reverse();
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
            Layer layer = data.mapLayer;
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
                    Layer layer = data.mapLayer;
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
            Layer layer = data.mapLayer;
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
            Layer layer = data.mapLayer;
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

        public bool EntryExists(Layer layer, HexCoord hexCoord)
        {
            if (!dictTileDatas.ContainsKey(layer))
                return false;
            if (!dictTileDatas[layer].ContainsKey(hexCoord))
                return false;
            return true;
        }

        public Dictionary<Layer, Dictionary<HexCoord, TileData>> GetTileRepresentation()
        {
            return dictTileDatas;
        }

        public void Clear()
        {
            dictTileDatas.Clear();
        }

        public void GetLayers(out List<Layer> layers)
        {
            layers = new();
            layers.AddRange(dictTileDatas.Keys);
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
        public int LayerSize(Layer layer)
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
            foreach ((var layer, Dictionary<HexCoord, TileData> value) in dictTileDatas)
            {
                size += value.Count;
            }
            return size;
        }
    }
}
