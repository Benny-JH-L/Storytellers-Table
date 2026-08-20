using Assets.StorytellersTable.Core.Map;
using StorytellersTable.Map;
using StorytellersTable.Utility.Log;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StorytellersTable
{
    public struct GridSelectorPayload
    {
        public HashSet<HexCoord> coords;
        public Layer activeLayer;
        public int layerRange;
    }

    public class HexGridSelector
    {
        [SerializeField] private bool allowFallThroughEmptyCells = true;

        private List<Layer> layersTopDown = new List<Layer>();  // cached, rebuild when levels change
        private List<Layer> layersBottomUp = new List<Layer>(); // cached, rebuild when levels change
        private MapTileRepresentation activeMapTileData => MapManager.Instance.ActiveMapData.mapTileData;

        public HexGridSelector() { }

        /// <summary>
        /// This should be called everytime the active map data changes.
        /// </summary>
        public void RebuildLevelOrder()
        {
            layersTopDown.Clear();
            layersBottomUp.Clear();

            activeMapTileData.GetLayers(out List<Layer> layers);
            
            layersTopDown.AddRange(layers);
            layersBottomUp.AddRange(layers);

            layersTopDown.Sort((a, b) => b.CompareTo(a));   // descending
            layersBottomUp.Sort((a, b) => a.CompareTo(a));  // ascending
        }

        /// <summary>
        /// Picks a hex cell under the cursor. Returns true if a valid cell. [DEPRECATE THIS!]
        /// </summary>
        public bool TryPick(int actLevel, out Layer hitLayer, out HexCoord hitCoord, out TileData tile)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            Layer activelayer = new (actLevel);

            // iteracte through active map's layer
            foreach (var layer in layersTopDown)
            {
                if (layer > activelayer)
                    continue; // skip floors above the active one entirely

                //float planeY = layer * levelHeight;
                float planeY = layer.Y();
                Plane plane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));

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

        /// <summary>
        /// Uses <paramref name="cam"/> position & mouse raycast to get the active layer, will ignore the same value in <paramref name="payload"/>.
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="cam"></param>
        public bool PickWithCamera(GridSelectorPayload payload, Camera cam, out List<TileData> result)
        {
            if (GetLayerFromCameraRaycast(cam, out Layer layer))
            {
                Layer oldLayer = payload.activeLayer;

                payload.activeLayer = layer;
                Pick(payload, out result);

                payload.activeLayer = oldLayer;         // set layer back
                return true;
            }

            // finding a layer with camera raycasting did not succeed
            WarningOut.Log(this, "Could not get layer with Raycast...");
            result = new();
            return false;
        }

        /// <summary>
        /// Gets the active layer based on the raycast of the <paramref name="camera"/>.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool GetLayerFromCameraRaycast(Camera camera, out Layer result)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            List<Layer> layersToIterateThrough;

            // camera is looking down
            if (Camera.main.transform.rotation.x >= 0f) 
            {
                layersToIterateThrough = layersTopDown;
            }
            // camera is looking up
            else
            {
                layersToIterateThrough = layersBottomUp;
            }

            // iterate through layers
            foreach (var layer in layersToIterateThrough)
            {
                float planeY = layer.Y();
                Plane plane = new(Vector3.up, new Vector3(0f, planeY, 0f));

                // raycast onto plane until a hit
                if (!plane.Raycast(ray, out float dist))
                    continue;

                // get hit point and convert to a axial coord
                Vector3 hitPoint = ray.GetPoint(dist);
                HexCoord candidate = HexMath.WorldToAxial(hitPoint);

                // if the hexcoord entry exists, return the layer that the hexcoord exists in
                if (activeMapTileData.EntryExists(layer, candidate))
                {
                    result = Layer.YToLayer(planeY);
                    return true;
                }
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Picks tile data's based on <paramref name="payload"/> and returns in <paramref name="result"/>.
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="result"></param>
        public void Pick(GridSelectorPayload payload, out List<TileData> result)
        {
            result = new();
            MapTileRepresentation tileRep = MapManager.Instance.ActiveMapData.mapTileData;
            int layerMax = payload.activeLayer.Val + payload.layerRange;
            int layerMin = payload.activeLayer.Val - payload.layerRange;

            // go through the input and add the "top most" tile data relative to the active layer, between the min and max layers.
            foreach (HexCoord hexCoord in payload.coords)
            {
                tileRep.GetTileDataStack(hexCoord, out List<TileData> datas, layerMax, layerMin);   // get's a "stack" of tile data's between the max and min layers
                result.Add(datas[0]);   // the first element will be the top most tile betwen the min and max
            }
        }
    }
}
