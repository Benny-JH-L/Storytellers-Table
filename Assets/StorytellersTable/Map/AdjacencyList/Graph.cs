
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StorytellersTable.Core.Data
{
    /// <summary>
    /// Class to store tiles, and represents map as an adjacency list.
    /// </summary>
    public class Graph
    {
        private Dictionary<HexCoord, GameObject> _tiles; // stores the tile located at a hex coordinate.

        public Graph()
        {
            _tiles = new Dictionary<HexCoord, GameObject>();
        }

        /// <summary>
        /// To get, `Graph[HexCoord]`, returns a GameObject (with `TileComponent`) if it exists, null otherwise.
        /// To set, `Graph[HexCoord] = value`, value must be a game object with a TileComponent (& HexRenderer).
        /// </summary>
        /// <param name="hexCoord"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public GameObject this[HexCoord hexCoord]   // operator `[]`
        {
            get => _tiles.TryGetValue(hexCoord, out GameObject hexRenderer) ? hexRenderer : null;   // get value based on hexCoord
            set
            {
                // set value (as long its a HexRenderer & TileComponent) based on hexCoord
                if (value.TryGetComponent<HexRenderer>(out HexRenderer _) && value.TryGetComponent<TileComponent>(out TileComponent __))
                    _tiles[hexCoord] = value;
                else
                    throw new ArgumentException("Invalid tile: GameObject must contain `TileComponent` & `HexRenderer` components.");
            }
        }

        /// <summary>
        /// Instead of using the class HexCoord, use q and r axial coordinates directly.
        /// </summary>
        /// <param name="q"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public GameObject this[int q, int r]
        {
            get => this[new HexCoord(q, r)];
            set => this[new HexCoord(q, r)] = value;
        }

        /// <summary>
        /// Returns a list of adjacent placed tiles relative to the hex coordinate.
        /// </summary>
        /// <param name="hexCoord"></param>
        /// <returns></returns>
        public List<GameObject> GetAdjacentTiles(HexCoord hexCoord)
        {
            // We can compute in O(1) time the adjacent hex tiles instead having to store them
            List<GameObject> result = new List<GameObject>();

            foreach (HexCoord offset in HexCoord.ADJACENT_TILE_OFFSETS)
            {
                GameObject obj = this[hexCoord + offset];
                if (obj != null)
                    result.Add(obj);
            }

            return result;
        }

        /// <summary>
        /// </summary>
        /// <param name="hexCoord"></param>
        /// <returns>A list of HexCoord that are adjacent to `hexCoord` parameter.</returns>
        public List<HexCoord> GetAdjacentHexCoords(HexCoord hexCoord)
        {
            // We can compute in O(1) time the adjacent hex tiles instead having to store them
            List<HexCoord> result = new List<HexCoord>();

            foreach (HexCoord offset in HexCoord.ADJACENT_TILE_OFFSETS)
                result.Add(hexCoord + offset);

            return result;
        }


        /// <summary>
        /// Same as the `Dictonary` TryGetValue() function.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="gameObj"></param>
        /// <returns></returns>
        public bool TryGetValue(HexCoord key, out GameObject gameObj)
        {
            return _tiles.TryGetValue(key, out gameObj);
        }

        /// <summary>
        /// Destroys all tiles (GameObjects) stored in the adjacency list, and clears it.
        /// </summary>
        public void Clear()
        {
            foreach (KeyValuePair<HexCoord, GameObject> pair in _tiles)
            {
                if (pair.Value != null)
                    UnityEngine.Object.Destroy(pair.Value);
            }
            _tiles.Clear();
        }

        /// <summary>
        /// Calls the `DrawMesh()` on every tile stored in the adjacency list.
        /// </summary>
        public void ReDrawTileMesh()
        {
            foreach (KeyValuePair<HexCoord, GameObject> pair in _tiles)
                pair.Value.GetComponent<HexRenderer>().DrawMesh();
        }
    }
}
