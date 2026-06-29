
using System.Collections.Generic;

namespace StorytellersTable.Core.Data
{
    /// <summary>
    /// Graph representation as an adjacency list.
    /// </summary>
    public static class Graph
    {

        /// <summary>
        /// Get's adjacent HexCoords based on `hexCoord` and `checkExists`.
        /// If `checkExists` is true, it will only include HexCoords that exist in `mapData`, ie tile data that exists at the HexCoord.
        /// </summary>
        /// <param name="mapData"></param>
        /// <param name="hexCoord"></param>
        /// <param name="checkExists"></param>
        /// <returns>Return's a list of HexCoords.</returns>
        public static List<HexCoord> GetAdjacentHexCoords(MapData mapData, HexCoord hexCoord, bool checkExists = false)
        {
            // We can compute in O(1) time the adjacent hex tiles instead having to store them
            List<HexCoord> result = new List<HexCoord>();

            foreach (HexCoord offset in HexCoord.ADJACENT_TILE_OFFSETS)
            {
                // don't check if the tile exists on the map
                if (!checkExists)
                    result.Add(hexCoord + offset);
                // check if the tile exists (not null) on the map
                else if (checkExists && mapData.tileDatas.TryGetValue(hexCoord + offset, out TileData data))
                    result.Add(hexCoord + offset);
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hexCoord"></param>
        /// <returns>A list of HexCoord that are adjacent to `hexCoord` parameter.</returns>
        /// 

        //private Dictionary<HexCoord, GameObject> _tiles = new Dictionary<HexCoord, GameObject>(); // stores the tile located at a hex coordinate.

        ///// <summary>
        ///// To get, `Graph[HexCoord]`, returns a GameObject (with `TileComponent`) if it exists, null otherwise.
        ///// To set, `Graph[HexCoord] = value`, value must be a game object with a TileComponent (& HexRenderer).
        ///// </summary>
        ///// <param name="hexCoord"></param>
        ///// <returns></returns>
        ///// <exception cref="ArgumentException"></exception>
        //public GameObject this[HexCoord hexCoord]   // operator `[]`
        //{
        //    get => _tiles.TryGetValue(hexCoord, out GameObject hexRenderer) ? hexRenderer : null;   // get value based on hexCoord
        //    set
        //    {
        //        // set value (as long its a HexRenderer & TileComponent) based on hexCoord
        //        if (value.TryGetComponent<HexRenderer>(out HexRenderer _) && value.TryGetComponent<TileComponent>(out TileComponent __))
        //            _tiles[hexCoord] = value;
        //        else
        //            throw new ArgumentException("Invalid tile: GameObject must contain `TileComponent` & `HexRenderer` components.");
        //    }
        //}

        ///// <summary>
        ///// Instead of using the class HexCoord, use q and r axial coordinates directly.
        ///// </summary>
        ///// <param name="q"></param>
        ///// <param name="r"></param>
        ///// <returns></returns>
        //public GameObject this[int q, int r]
        //{
        //    get => this[new HexCoord(q, r)];
        //    set => this[new HexCoord(q, r)] = value;
        //}

        ///// <summary>
        ///// Returns a list of adjacent placed tiles relative to the hex coordinate.
        ///// </summary>
        ///// <param name="hexCoord"></param>
        ///// <returns></returns>
        //public List<GameObject> GetAdjacentTiles(HexCoord hexCoord)
        //{
        //    // We can compute in O(1) time the adjacent hex tiles instead having to store them
        //    List<GameObject> result = new List<GameObject>();

        //    foreach (HexCoord offset in HexCoord.ADJACENT_TILE_OFFSETS)
        //    {
        //        GameObject obj = this[hexCoord + offset];
        //        if (obj != null)
        //            result.Add(obj);
        //    }

        //    return result;
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="hexCoord"></param>
        ///// <returns>A list of HexCoord that are adjacent to `hexCoord` parameter.</returns>
        //public List<HexCoord> GetAdjacentHexCoords(HexCoord hexCoord)
        //{
        //    // We can compute in O(1) time the adjacent hex tiles instead having to store them
        //    List<HexCoord> result = new List<HexCoord>();

        //    foreach (HexCoord offset in HexCoord.ADJACENT_TILE_OFFSETS)
        //        result.Add(hexCoord + offset);

        //    return result;
        //}


        ///// <summary>
        ///// Same as the `Dictonary` TryGetValue() function.
        ///// </summary>
        ///// <param name="key"></param>
        ///// <param name="gameObj"></param>
        ///// <returns></returns>
        //public bool TryGetValue(HexCoord key, out GameObject gameObj)
        //{
        //    return _tiles.TryGetValue(key, out gameObj);
        //}

        ///// <summary>
        ///// Destroys all tiles (GameObjects) stored in the adjacency list, and clears it.
        ///// </summary>
        //public void Clear()
        //{
        //    foreach (KeyValuePair<HexCoord, GameObject> pair in _tiles)
        //    {
        //        if (pair.Value != null)
        //            UnityEngine.Object.Destroy(pair.Value);
        //    }
        //    _tiles.Clear();
        //}

        ///// <summary>
        ///// Calls the `DrawMesh()` on every tile stored in the adjacency list.
        ///// </summary>
        //public void ReDrawTileMesh()
        //{
        //    foreach (KeyValuePair<HexCoord, GameObject> pair in _tiles)
        //        pair.Value.GetComponent<HexRenderer>().DrawMesh();
        //}

        //public int Size()
        //{
        //    return _tiles.Count;
        //}
    }

}
