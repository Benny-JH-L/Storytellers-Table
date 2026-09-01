using Assets.StorytellersTable.Core.Map;
using StorytellersTable.Campaign.Modes;
using StorytellersTable.Map;
using StorytellersTable.Utility.Log;
using StorytellersTable.Utility.Printer;
using System.Collections.Generic;
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
        public bool layerFocus; // focus selection on `initialLayer`, disables dynamic layer selection
        public bool filterWithMapRep;   // true: filters results out if it exists in `mapTileRepresentation`

        // Specific for mode type
        public uint radius;     // radial & draw select
        //public AreaSelectPayload areaSelectPayload; // area select
        public AreaSelectionContainer areaSelctContainer;   // area select NOT USED

        public static GridSelectorPayload Copy(GridSelectorPayload payload)
        {
            GridSelectorPayload copy = new()
            {
                mode = payload.mode,
                mapTileRepresentation = payload.mapTileRepresentation,
                initialHexCoord = payload.initialHexCoord,
                initialLayer = payload.initialLayer,
                layerRange = payload.layerRange,
                layerFocus = payload.layerFocus,
                radius = payload.radius,
                //areaSelectPayload = payload.areaSelectPayload
                areaSelctContainer = payload.areaSelctContainer
            };
            return copy;
        }

        public override string ToString()
        {
            return $"Mode: {mode}\n" +
                $"mapTileRepresentation: {mapTileRepresentation}\n" +
                $"InitialHexCoord: {initialHexCoord}\n" +
                $"initialLayer: {initialLayer}\n" +
                $"layerRange: {layerRange}\n" +
                $"layerFocus: {layerFocus}\n" +
                $"radius: {radius}\n" +
                //$"areaSelectData: {areaSelectPayload}\n";
                $"areaSelectData: {areaSelctContainer}\n";
        }
    }

    public class HexGridSelector
    {
        [SerializeField] private uint numSlices = 5;  // number of slice in a layer to check, used to enhance accuracy when raycasting with the camera to find the layer
        private readonly List<Layer> layersTopDown = new List<Layer>();  // cached, rebuild when levels change
        private readonly List<Layer> layersBottomUp = new List<Layer>(); // cached, rebuild when levels change
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
        /// Uses the <paramref name="cam"/> and mouse position to raycast to get the initial layer and hexcoord. The same values
        /// in <paramref name="payload"/> will be ignored.
        /// </summary>
        /// 
        /// <remarks>
        /// Return's a copy of the original payload, <paramref name="payload"/>, but replaces values with the ones it used.
        /// </remarks>
        /// <param name="payload"></param>
        /// <param name="cam"></param>
        /// <param name="tileDataResult"></param>
        /// <param name="coordResult"></param>
        /// <param name="updatedPayload"></param>
        /// <returns></returns>
        public bool PickWithCamera(GridSelectorPayload payload, Camera cam, out HashSet<TileData> tileDataResult, out HashSet<HexCoord> coordResult, out GridSelectorPayload updatedPayload)
        {
            if (GetLayerFromCameraRaycast(cam, out Layer layer, out HexCoord hitCoord))
            {
                updatedPayload = GridSelectorPayload.Copy(payload);

                // Set new values
                updatedPayload.initialHexCoord = hitCoord;
                updatedPayload.initialLayer = layer;

                //DebugOut.Log(this, $"{updatedPayload}");

                bool isSuccess = Pick(updatedPayload, out tileDataResult, out coordResult);
                return isSuccess;
            }

            // finding a layer with camera raycasting did not succeed
            WarningOut.Log(this, "Could not get layer with Raycast...");
            tileDataResult = new();
            coordResult = new();
            updatedPayload = new();
            return false;
        }

        /// <summary>
        /// Gets a layer based on the raycast of the <paramref name="camera"/> and mouse position, 
        /// return's the hexcoord, <paramref name="hexCoordResult"/>, and layer, <paramref name="layerResult"/>, it hits.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="layerResult"></param>
        /// <param name="hexCoordResult"></param>
        /// <returns></returns>
        private bool GetLayerFromCameraRaycast(Camera camera, out Layer layerResult, out HexCoord hexCoordResult)
        {
            layerResult = default;
            hexCoordResult = default;
            
            Ray ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());

            // Ignore rays parallel to horizontal layer planes (D.y == 0)
            if (Mathf.Approximately(ray.direction.y, 0f)) 
                return false;

            // Evaluate direction vector D.y directly instead of Quaternion rotation components.
            // If D.y < 0 (pointing downward), higher plane heights Y yield smaller distance t along ray.
            // Iterate top-down to evaluate closest planes first.
            List<Layer> layersToIterateThrough = (ray.direction.y < 0f) ? layersTopDown : layersBottomUp;

            float minRayDistance = float.MaxValue;
            bool hitFound = false;
            float sliceSize = (float)Singleton.Instance.height / (float)numSlices;

            // Iterate across all layers to find the true foreground intersection
            foreach (var layer in layersToIterateThrough)
            {
                float layerTop = layer.Y(); // get the layer's surface position

                // Check top surface (i = 0) and vertical volume slices (i = 1 .. numSlices), should be checking `numSlices + 1` times
                for (int i = 0; i <= numSlices; i++)
                {
                    float sliceY = layerTop - (i * sliceSize);
                    Plane plane = new (Vector3.up, new Vector3(0f, sliceY, 0f));

                    if (!plane.Raycast(ray, out float dist))
                        continue;

                    // get hit point and convert to hex coord
                    Vector3 hitPoint = ray.GetPoint(dist);
                    HexCoord candidateCoord = HexMath.WorldToAxial(hitPoint);

                    // Check if tile exists in the map data at this layer and hex coord
                    if (activeMapTileData.EntryExists(layer, candidateCoord))
                    {
                        // The visual foreground corresponds to min(dist) along the camera ray
                        if (dist < minRayDistance)
                        {
                            minRayDistance = dist;
                            layerResult = layer;
                            hexCoordResult = candidateCoord;
                            hitFound = true;
                        }
                    }
                }
            }

            if (hitFound)
            {
                //DebugOut.Log(this, $"Selected Layer: {layerResult} @ Coord: {hexCoordResult} (Ray Distance: {minRayDistance})");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Picks hex coordinates and tile datas based on <paramref name="payload"/> and returns it as <paramref name="pickedHexCoords"/> and <paramref name="tileDataResult"/>, respectively.
        /// </summary>
        /// 
        /// <remarks>
        /// <paramref name="pickedHexCoords"/> will contain all the hex coordinates that it considered.
        /// <paramref name="tileDataResult"/> will contain tile data's that exists in the <paramref name="payload"/>'s MapTileRepresentation and if its hex coordinate was considered.
        /// </remarks>
        /// 
        /// <param name="payload"></param>
        /// <param name="tileDataResult"></param>
        /// <param name="pickedHexCoords"></param>
        /// <returns></returns>
        public bool Pick(GridSelectorPayload payload, out HashSet<TileData> tileDataResult, out HashSet<HexCoord> pickedHexCoords)
        {
            tileDataResult = new();
            pickedHexCoords = new();

            // Based on the selection mode, get initial hex coords
            HexCoord initialCoord = payload.initialHexCoord;
            HashSet<HexCoord> initialCoordSet = new() { initialCoord };
            //AreaSelectPayload areaPayload = payload.areaSelectPayload;
            AreaSelectionContainer areaContainer = payload.areaSelctContainer;

            if (!Validate(payload))
            {
                ErrorOut.Log(this, "GridSelectorPayload invalid");

                //if (payload.initialLayer is not null && payload.mapTileRepresentation is not null)
                //{
                //    payload.mapTileRepresentation.TryGet(payload.initialLayer, payload.initialHexCoord, out var data);
                //    tileDataResult.Add(data);
                //}

                return false;
            }
            //DebugOut.Log(this, $"paylod used: {payload}");

            MapTileRepresentation tileRep = payload.mapTileRepresentation;

            switch (payload.mode)
            {
                case SelectModeTypes.radialSelect:
                    HexMath.GetHexRingArea(initialCoord, (int)payload.radius, initialCoordSet);
                    break;
                case SelectModeTypes.areaSelect:
                //    HexMath.GetAreaAxial(areaPayload.Start, areaPayload.End, initialCoordSet);
                // TODO: do i move stuff from `area selection container` to here...
                    break;
                case SelectModeTypes.drawSelect:
                    HexMath.GetHexRingArea(initialCoord, (int)payload.radius, initialCoordSet);
                    break;
                // adds the initial coord into the set (which is done above)
                case SelectModeTypes.singleSelect:
                    break;
            }

            // filter the initial coords based on the edit mode and map data
            //Printer.Print(initialCoordSet.ToList(), "before: ");  // debug

            // TODO: Add hex coords from the range???

            if (payload.filterWithMapRep)
                FilterInitialSet(payload.mapTileRepresentation, initialCoordSet);   // set should only contain hexcoords that exist on the given map 
            pickedHexCoords.UnionWith(initialCoordSet); // add set to result

            //Printer.Print(coordResult.ToList(), "after: ");  // debug

            // Get tile data's if they exist
            foreach (HexCoord coord in pickedHexCoords)
            {
                if (tileRep.TryGet(payload.initialLayer, coord, out TileData data))
                    tileDataResult.Add(data);
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
            HashSet<HexCoord> removeSet = new();
            // Go through each coord
            foreach (HexCoord hexCoord in set)
            {
                bool entryFound = false;
                // check if that coord exists in any layer
                foreach ((Layer layer, var dict) in mapTileRep.GetTileRepresentation())
                {
                    if (mapTileRep.EntryExists(layer, hexCoord))
                    {
                        entryFound = true;
                        break;
                    }
                }

                // if the coord does not exist in any layer, remove it
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
            if (payload.mapTileRepresentation is null)
                return false;

            if (payload.initialLayer is null)
                return false;

            //if (payload.mode == SelectModeTypes.areaSelect)
            //{
            //    if (!payload.areaSelectPayload.IsValid())
            //        return false;
            //}

            return true;
        }
    }
}
