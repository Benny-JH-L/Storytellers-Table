
using StorytellersTable.Core.Data;
using StorytellersTable.Map;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StorytellersTable
{
    public class HexGridSelector
    {
        [SerializeField] private bool allowFallThroughEmptyCells = true;

        private List<int> layersTopDown = new List<int>(); // cached, rebuild when levels change
        private MapTileRepresentation activeMapTileData => MapManager.Instance.ActiveMapData.mapTileData;

        public HexGridSelector() { }

        public void RebuildLevelOrder()
        {
            layersTopDown.Clear();
            layersTopDown.AddRange(activeMapTileData.GetTileRepresentation().Keys);
            layersTopDown.Sort((a, b) => b.CompareTo(a)); // descending
        }

        /// <summary>
        /// Picks a hex cell under the cursor. Returns true if a valid cell.
        /// </summary>
        public bool TryPick(int activeLevel, out int hitLayer, out HexCoord hitCoord, out TileData tile)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

            // iteracte through active map's layer
            foreach (int layer in layersTopDown)
            {
                if (layer > activeLevel)
                    continue; // skip floors above the active one entirely

                //float planeY = layer * levelHeight;
                float planeY = layer;
                var plane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));

                if (!plane.Raycast(ray, out float dist)) 
                    continue;

                Vector3 hitPoint = ray.GetPoint(dist);
                HexCoord candidate = HexMath.WorldToAxial(hitPoint);

                // check if the entry exists in the map
                if (activeMapTileData.EntryExists(layer, candidate))
                {
                    hitLayer = layer;
                    hitCoord = candidate;
                    tile = activeMapTileData.GetTileRepresentation()[layer][candidate];
                    return true;
                }
            }

            hitLayer = default;
            hitCoord = default;
            tile = null;
            return false;
        }
    }
}
