using Assets.StorytellersTable.Core.Map;
using StorytellersTable.Campaign.Modes;
using StorytellersTable.Map;
using StorytellersTable.Utility.Log;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StorytellersTable
{
    public struct GridSelectorPayload
    {
        public SelectModeTypes mode;
        public MapTileRepresentation mapTileRepresentation;
        public HexCoord initialHexCoord;
        public Layer initialLayer;
        public uint layerRange;

        // Specific for mode type
        public uint radius;     // radial & draw select
        public AreaEditData areaSelectData; // area select
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
            layersBottomUp.Sort((a, b) => a.CompareTo(b));  // ascending
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
        /// Uses the <paramref name="cam"/> and mouse position to raycast to get the initial layer and hexcoord. The same values
        /// in <paramref name="payload"/> will be ignored.
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="cam"></param>
        /// <param name="tileDataResult"></param>
        /// <param name="coordResult"></param>
        /// <returns></returns>
        public bool PickWithCamera(GridSelectorPayload payload, Camera cam, out List<TileData> tileDataResult, out List<HexCoord> coordResult)
        {
            if (GetLayerFromCameraRaycast(cam, out Layer layer, out HexCoord hitCoord))
            {
                Layer oldLayer = payload.initialLayer;
                HexCoord oldHex = payload.initialHexCoord;

                payload.initialHexCoord = hitCoord;
                payload.initialLayer = layer;
                bool isSuccess = Pick(payload, out tileDataResult, out coordResult);

                // Set initial values back
                payload.initialHexCoord = oldHex;
                payload.initialLayer = oldLayer;
                return isSuccess;
            }

            // finding a layer with camera raycasting did not succeed
            WarningOut.Log(this, "Could not get layer with Raycast...");
            tileDataResult = new();
            coordResult = new();
            return false;
        }

        /// <summary>
        /// Gets the active layer based on the raycast of the <paramref name="camera"/> and mouse position, 
        /// return's the hexcoord, <paramref name="hexCoord"/>, it hits.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="result"></param>
        /// <param name="hexCoord"></param>
        /// <returns></returns>
        private bool GetLayerFromCameraRaycast(Camera camera, out Layer result, out HexCoord hexCoord)
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
                    hexCoord = candidate;
                    return true;
                }
            }

            result = default;
            hexCoord = default;
            return false;
        }

        /// <summary>
        /// Picks tile data's based on <paramref name="payload"/> and returns in <paramref name="tileDataResult"/>.
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="tileDataResult"></param>
        /// <param name="coordResult"></param>
        public bool Pick(GridSelectorPayload payload, out List<TileData> tileDataResult, out List<HexCoord> coordResult)
        {
            tileDataResult = new();
            coordResult = new();

            if (!Validate(payload))
            {
                WarningOut.Log(this, "GridSelectorPayload invalid");
                return false;
            }

            int layerMax = payload.initialLayer.Val + (int)payload.layerRange;
            int layerMin = payload.initialLayer.Val - (int)payload.layerRange;

            // Based on the selection mode, get initial hex coords
            HexCoord initialCoord = payload.initialHexCoord;
            HashSet<HexCoord> initialCoordSet = new() { initialCoord };
            switch (payload.mode)
            {
                case SelectModeTypes.radialSelect:
                    HexMath.GetHexRingArea(initialCoord, (int)payload.radius, initialCoordSet);
                    break;
                case SelectModeTypes.areaSelect:
                    WarningOut.Log(this, "area select not implemented");
                    //HexMath.GetAreaAxial();
                    break;
                case SelectModeTypes.drawSelect:
                    HexMath.GetHexRingArea(initialCoord, (int)payload.radius, initialCoordSet);
                    break;
                // adds the initial coord into the set (which is done above)
                case SelectModeTypes.singleSelect:
                    break;
            }

            // filter the initial coords based on the edit mode and map data
            FilterInitialSet(payload.mapTileRepresentation, initialCoordSet);   // set should only contain hexcoords that exist on the map given

            // Go through the coord set and get the "top most" tile data relative to the active layer, between the min and max layers.
            MapTileRepresentation tileRep = payload.mapTileRepresentation;
            foreach (HexCoord hexCoord in initialCoordSet)
            {
                tileRep.GetTileDataStack(hexCoord, out List<TileData> tileDatas, layerMax, layerMin);

                // the first element will be the top most tile betwen the min and max
                if (tileDatas.Count > 0)
                    tileDataResult.Add(tileDatas[0]);

                coordResult.Add(hexCoord);
            }

            return true;
        }


        /// <summary>
        /// Filters the <paramref name="set"/> based on if a hexcoord exists in <paramref name="mapTileRep"/>, regardless of the layer.
        /// </summary>
        /// <param name="mapTileRep"></param>
        /// <param name="set"></param>
        private void FilterInitialSet(MapTileRepresentation mapTileRep, HashSet<HexCoord> set)
        {
            // remove the hexcoords that do not exist in the map data
            HashSet<HexCoord> removeSet = new ();
            foreach (HexCoord hexCoord in set)
            {
                bool entryFound = false;
                foreach ((Layer layer, var dict) in mapTileRep.GetTileRepresentation())
                {
                    if (mapTileRep.EntryExists(layer, hexCoord))
                    {
                        entryFound = true;
                        break;
                    }
                }

                if (!entryFound)
                    removeSet.Add(hexCoord);
            }

            // remove coords
            set.RemoveWhere(hexCoord => removeSet.Contains(hexCoord));
        }

        /// <summary>
        /// Validates the payload. Return true if valid, false otherwise.
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        private bool Validate(GridSelectorPayload payload)
        {
            if (payload.mapTileRepresentation == null)
                return false;

            if (payload.initialLayer == null)
                return false;

            return true;
        }
    }
}
