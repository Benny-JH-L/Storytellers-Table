
using StorytellersTable.Core.Data;
using StorytellersTable.Map;
using StorytellersTable.Utility.Log;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StorytellersTable.Campaign.Modes
{
    /// <summary>
    /// Encapsulates behavior while modifying the map tiles; layout coordinates, layered tile placement, and geometry.
    /// </summary>
    public class MapEditMode : ICampaignMode
    {
        // Raycast to this layer to place tiles
        public static LayerMask mapEditLayerMask = LayerMask.GetMask("MapEditPlane");
        public static float raycastMaxDistance = 500f;

        [Header("Tile Settings")]
        public static float outerSize = 1f;
        public static float innerSize = 0f;
        public static float height = 1f;
        public static bool isFlatTopped;
        public static Material placedMaterial;       // material of placed tiles --> set based on UI
        public static Material ghostMaterial;

        private readonly GameObject _uiPrefab;
        private readonly Transform _uiParentTransform;
        private readonly MapEditAction _inputMap;

        [SerializeField] private GameObject _runtimeUiInstance; // UI for the map edit mode
        public MapData activeMap => MapManager.Instance.ActiveMapData;

        // tiles that are not placed, where potential placement is visually shown.
        [SerializeField] private List<HexCoord> _unconfirmedTilePos;
        [SerializeField] private List<HexRenderer> _unconfirmedTileVisuals;
        [SerializeField] private GameObject _unconfirmedTileVisualsParent;  // ghost tiles will be parented to this

        // track different placement types (only one may be enabled at a time)
        private bool _radialOn, _areaOn, _drawOn;

        public MapEditMode(GameObject uiPrefab, Transform uiParent, MapEditAction inputMap)
        {
            _uiPrefab = uiPrefab;
            _uiParentTransform = uiParent;
            _inputMap = inputMap;

            _runtimeUiInstance = null;

            _unconfirmedTilePos = new List<HexCoord>();
            _unconfirmedTileVisuals = new List<HexRenderer>();
            _unconfirmedTileVisualsParent = new GameObject("MapEdit - Unconfirmed_Tile_Visuals");
            _unconfirmedTileVisualsParent.transform.SetParent(CampaignModeManager.Instance.transform, true);

            // Set initial materials
            placedMaterial = Singleton.Instance.defaultTileMaterial;
            ghostMaterial = Singleton.Instance.ghostTileMaterial;

            _radialOn = false;
            _areaOn = false;
            _drawOn = false;

            // add callback to toggle radial, area, and draw tile placements
            _inputMap.Edit.EnableRadial.performed += ToggleRadial;
            _inputMap.Edit.EnableArea.performed += ToggleArea;
            _inputMap.Edit.EnableDraw.performed += ToggleDraw;
            // add call backs to input map...
        }

        void ICampaignMode.Enter()
        {
            // Instantiate UI if it does not exist
            if (_uiPrefab != null && _runtimeUiInstance == null)
                _runtimeUiInstance = Object.Instantiate(_uiPrefab, _uiParentTransform);
            // logic to get the active scene's MapBase...

            _inputMap.Enable();
        }

        void ICampaignMode.Exit()
        {
            _inputMap.Disable();            // disable input for this mode

            if (_runtimeUiInstance != null) // clean up
            {
                Object.Destroy(_runtimeUiInstance);
                _runtimeUiInstance = null;
            }

            // Clean up ghost tiles
            _DestroyUnconfirmedTiles();
        }

        void ICampaignMode.UpdateMode()
        {
            if (Keyboard.current.lKey.wasPressedThisFrame)
                DebugOut.Log(this, "Map edit - hi!!");

            // Place unconfirmed tiles
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                PlaceUnconfirmedTiles();
                return;
            }

            _DestroyUnconfirmedTiles();

            // Get mouse's hex coordinate based on world position 
            HexCoord mouseHexCoord;
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit, raycastMaxDistance, mapEditLayerMask)) // right now this only works when it hits something in the world, try to use the `plane` strat instead.
            {
                mouseHexCoord = WorldToAxial(hit.point);
                //Debug.Log($"Hit Point: {hit.point} | Mouse Axial: {mouseHexCoord}");

                // Check if there's existing tile data (non null) at that hex position of the map
                if (activeMap.tileDatas.TryGetValue(mouseHexCoord, out TileData tileData))
                {
                    //DebugOut.Log(this, $"Hovering over tile: {tileData.ToString()}");


                    // OLD
                    //TileComponent tileComp = tileData.GetComponent<TileComponent>();
                    //Debug.Log($"Hover over | existing tile: {tileComp.GetData<TileBase>().GetType().Name} at coord: {tileComp.HexCoord}");

                    // highlight the tile
                    //_highlightTile(tileComp);
                }
                // No tile exists at the specified hex coord
                else
                {
                    //WarningOut.Log(this, $"Hit registered at {mouseHexCoord}, but no matching tile key was found in the dictionary.");

                    // Add unconfirmed position
                    _unconfirmedTilePos.Add(mouseHexCoord);

                    // Create ghost visual at mouse's hex coord
                    GenerateGhostTile(mouseHexCoord, ghostMaterial);

                    //GameObject newTile = _GenerateTile(mouseHexCoord, ghostMaterial);
                    //_unconfirmedTiles.Add(newTile);
                }
            }
            // If we can't get the mouse's world position do nothing.
            else
                return;

            // For special placement settings: Radial, Area, and Draw.

            if (_radialOn)
            {
                // compute additional "unconfirmed" tiles to potentially place, add it to the list, _unconfirmedTiles!

            }
            else if (_areaOn)
            {
                // compute additional "unconfirmed" tiles to potentially place, add it to the list, _unconfirmedTiles!

            }
            else if (_drawOn)
            {
                // compute additional "unconfirmed" tiles to potentially place, add it to the list, _unconfirmedTiles!

            }
        }

        /// <summary>
        /// Generate a ghost visual at a given HexCoord with a Material.
        /// </summary>
        /// <param name="hexCoord"></param>
        /// <param name="ghostMat"></param>
        private void GenerateGhostTile(HexCoord hexCoord, Material ghostMat)
        {
            HexRenderer ghostVisual = GenerateHexRenderer(hexCoord, ghostMat);
            ghostVisual.transform.SetParent(_unconfirmedTileVisualsParent.transform, true);
            _unconfirmedTileVisuals.Add(ghostVisual);
        }

        /// <summary>
        /// Places all unconfirmed tiles to the active map.
        /// </summary>
        private void PlaceUnconfirmedTiles()
        {
            foreach (HexCoord tileCoord in _unconfirmedTilePos)
            {
                // Set `placed` material
                TileData newData = new TileData(tileCoord, GetPositionFromAxial(tileCoord).y); // ensure you include other fields...
                MapManager.Instance.AddToActiveMap(newData);

                //TileComponent tileComp = tile.GetComponent<TileComponent>();
                //tileComp.HexRenderer.SetMaterial(placedMaterial);

                //// Set parent of tile and add the tile to the active map 
                ////tile.transform.SetParent(activeMap.transform, true);
                //tile.transform.SetParent(MapManager.Instance.transform, true);
                //activeMap.graph[tileComp.tileData.hexCoord] = tile;
            }

            // Clean up, MapManger will generate the placed tiles' visuals
            _DestroyUnconfirmedTiles();

            return;
        }

        /// <summary>
        /// Destroys the list of unconfirmed tile visuals and positions.
        /// </summary>
        private void _DestroyUnconfirmedTiles()
        {
            foreach (HexRenderer hexRenderer in _unconfirmedTileVisuals)
            {
                if (hexRenderer != null)
                    Object.Destroy(hexRenderer.gameObject);
            }
            _unconfirmedTileVisuals.Clear();
            _unconfirmedTilePos.Clear();
        }

        //private void _highlightTile(TileComponent tileComp)
        //{
        //    // code here
        //    // could make a tile just slightly larger than the target tile and make it light a light blue shade that's like 20% opacity or something
        //    // which is enabled/disabled and moved to target tiles position
        //}

        /// <summary>
        /// Change the material of the next newly placed tiles.
        /// </summary>
        /// <param name="newMat"></param>
        public void SetPlacedMaterial(Material newMat)
        {
            placedMaterial = newMat;
        }

        /// <summary>
        /// Generates a map using q, and r. q is the length of the map, and r is the height of the map.
        /// </summary>
        /// <param name="mapManager"></param>
        /// <param name="mapSize"></param>
        public static void LayoutMap(MapManager mapManager, Vector2Int mapSize, Material mat)
        {
            Stopwatch sw = Stopwatch.StartNew();    // start timer

            // Generate a clean rectangular bound using axial loops
            for (int r = 0; r < mapSize.y; r++)
            {
                // Calculate the row offset dynamically to slice a straight vertical edge
                int offset = Mathf.FloorToInt(r / 2f);

                for (int q = -offset; q < mapSize.x - offset; q++)
                {
                    // Capture the exact true coordinates
                    HexCoord hexCoord = new HexCoord(q, r);

                    // If flat-topped, the coordinate mapping swaps columns/rows for the offset orientation
                    if (MapEditMode.isFlatTopped)
                    {
                        int qFlat = r;
                        int offsetFlat = Mathf.FloorToInt(qFlat / 2f);
                        int rFlat = q;
                        hexCoord = new HexCoord(qFlat, rFlat + offsetFlat);
                    }

                    // Generate tile data, then add it to the map. 
                    TileData newData = new TileData(hexCoord, GetPositionFromAxial(hexCoord).y); // ENSURE YOU ADD THE OTHER DETAILS!
                    mapManager.AddToActiveMap(newData);
                }
            }

            sw.Stop();  // stop timer
            DebugOut.Log(typeof(MapEditMode), $"LayoutMap() - elapsed time: {sw.Elapsed.TotalSeconds} seconds.");
        }

        #region Tile Generation & World <-> Hex conversions
        /// <summary>
        /// Generates a hex tile at the specified hex axial, q & r, coordinate, and places it at the converted world space position, 
        /// then adds it to the map's adjacency list.
        /// </summary>
        /// <param name="hexCoord"></param>
        /// <param name="mat"></param>
        /// <returns></returns>
        //private static GameObject _GenerateTile(HexCoord hexCoord, Material mat)
        //{
        //    // Create GameObject with HexRenderer and TileComponent
        //    GameObject tile = new GameObject($"Hex ({hexCoord.q},{hexCoord.r})", typeof(HexRenderer), typeof(TileComponent));
        //    tile.transform.position = _GetPositionFromAxial(hexCoord);

        //    // Set up HexRenderer
        //    HexRenderer hexRenderer = tile.GetComponent<HexRenderer>();
        //    hexRenderer.outerSize = outerSize;
        //    hexRenderer.innerSize = innerSize;
        //    hexRenderer.height = height;
        //    hexRenderer.isFlatTopped = isFlatTopped;
        //    hexRenderer.SetMaterial(mat);
        //    hexRenderer.DrawMesh();
        //    hexRenderer.SetHexText(hexCoord);

        //    // Set up and bridge the tile data context
        //    TileData tileData = new TileData(hexCoord);
        //    TileComponent tileComponent = tile.GetComponent<TileComponent>();
        //    tileComponent.Setup(tileData);

        //    return tile;
        //}

        public static HexRenderer GenerateHexRenderer(HexCoord hexCoord, Material mat)
        {
            HexRenderer hexRenderer = new GameObject($"Hex ({hexCoord.q},{hexCoord.r})", typeof(HexRenderer)).GetComponent<HexRenderer>();
            // Set up where the visual's position in the world
            hexRenderer.transform.position = GetPositionFromAxial(hexCoord);
            // Set up HexRenderer
            hexRenderer.outerSize = outerSize;
            hexRenderer.innerSize = innerSize;
            hexRenderer.height = height;
            hexRenderer.isFlatTopped = isFlatTopped;
            hexRenderer.SetMaterial(mat);
            hexRenderer.DrawMesh();

            return hexRenderer;
        }

        public static HexRenderer GenerateHexRenderer(Vector3 worldPos, Material mat)
        {
            return GenerateHexRenderer(WorldToAxial(worldPos), mat);
        }

        ///// <summary>
        ///// Generates a hex tile at the worldPos converted to specified hex axial, q & r, coordinate, and places it at the converted world space position, 
        ///// then adds it to the map's adjacency list.
        ///// </summary>
        ///// <param name="worldPos"></param>
        ///// <param name="mat"></param>
        ///// <returns></returns>
        //private static GameObject _GenerateTile(Vector3 worldPos, Material mat)
        //{
        //    return _GenerateTile(WorldToAxial(worldPos), mat);
        //}

        /// <summary>
        /// Computes the exact 3D world position from the hex coordinate using structural basis vector matrix transformations.
        /// This removes all floating point tracking gaps and anchors the origin natively at (0,0,0).
        /// </summary>
        public static Vector3 GetPositionFromAxial(HexCoord coord)
        {
            float xPosition = 0f;
            float zPosition = 0f;
            float size = outerSize;

            if (!isFlatTopped)
            {
                // Pointy-Topped Basis Matrix 
                xPosition = size * (Mathf.Sqrt(3f) * coord.q + Mathf.Sqrt(3f) / 2f * coord.r);
                zPosition = size * (3f / 2f * coord.r);
            }
            else
            {
                // Flat-Topped Basis Matrix
                xPosition = size * (3f / 2f * coord.q);
                zPosition = size * (Mathf.Sqrt(3f) / 2f * coord.q + Mathf.Sqrt(3f) * coord.r);
            }

            // Inverting the Z axis to maintain your layout structure starting from top-left progression
            return new Vector3(xPosition, 0f, -zPosition);
        }

        /// <summary>
        /// Converts a 3D world position (using X and Y) into a discrete integer Axial HexCoord.
        /// </summary>
        /// <param name="worldPos"></param>
        /// <returns></returns>
        public static HexCoord WorldToAxial(Vector3 worldPos)
        {
            float size = outerSize;
            float fracQ, fracR;
            float worldX = worldPos.x;
            float worldZ = -worldPos.z; // apply layout space restoration up front

            if (!isFlatTopped)
            {
                // Pointy-top matrix inversion transformation
                fracQ = (Mathf.Sqrt(3f) / 3f * worldX - 1f / 3f * worldZ) / size;
                fracR = (2f / 3f * worldZ) / size;
            }
            else
            {
                // Flat-top matrix inversion transformation
                fracQ = (2f / 3f * worldX) / size;
                fracR = (-1f / 3f * worldX + Mathf.Sqrt(3f) / 3f * worldZ) / size;
            }

            // Convert to 3D cube coordinates to do robust rounding (ensuring q + r + s = 0)
            float fracS = -fracQ - fracR;

            int q = Mathf.RoundToInt(fracQ);
            int r = Mathf.RoundToInt(fracR);
            int s = Mathf.RoundToInt(fracS);

            // Calculate the rounding deltas
            float qDiff = Mathf.Abs(q - fracQ);
            float rDiff = Mathf.Abs(r - fracR);
            float sDiff = Mathf.Abs(s - fracS);

            // Re-adjust the axis with the largest rounding error to satisfy q + r + s = 0
            if (qDiff > rDiff && qDiff > sDiff)
            {
                q = -r - s;
            }
            else if (rDiff > sDiff)
            {
                r = -q - s;
            }
            // (If sDiff is largest, no adjustments to q or r are required)

            return new HexCoord(q, r);
        }

        #endregion

        #region Input Action Callbacks

        /// <summary>
        /// Callback to toggle radial tile placement for an Input Action.
        /// </summary>
        /// <param name="context"></param>
        private void ToggleRadial(InputAction.CallbackContext context)
        {
            bool tmp = !_radialOn;
            DisableAllSpecialPlacement();
            _radialOn = tmp;

            if (!_radialOn)
            {
                DebugOut.Log(this, "Map edit - Radial disabled");
                return;
            }

            DebugOut.Log(this, "Map edit - Radial enabled");
        }

        /// <summary>
        /// Callback to toggle area tile placement for an Input Action.
        /// </summary>
        /// <param name="context"></param>
        private void ToggleArea(InputAction.CallbackContext context)
        {
            bool tmp = !_areaOn;
            DisableAllSpecialPlacement();
            _areaOn = tmp;

            if (!_areaOn)
            {
                DebugOut.Log(this, "Map edit - Area disabled");
                return;
            }

            DebugOut.Log(this, "Map edit - Area enabled");
        }

        /// <summary>
        /// Callback to toggle draw tile placement for an Input Action.
        /// </summary>
        /// <param name="context"></param>
        private void ToggleDraw(InputAction.CallbackContext context)
        {
            bool tmp = !_drawOn;
            DisableAllSpecialPlacement();
            _drawOn = tmp;

            if (!_drawOn)
            {
                DebugOut.Log(this, "Map edit - draw disabled");
                return;
            }

            DebugOut.Log(this, "Map edit - draw enabled");
        }

        /// <summary>
        /// Disables radial, area, and draw placement modes, single placement is unaffected.
        /// </summary>
        private void DisableAllSpecialPlacement()
        {
            _radialOn = false;
            _areaOn = false;
            _drawOn = false;
        }

        // functions for Input Action call backs...

        #endregion
    }
}
