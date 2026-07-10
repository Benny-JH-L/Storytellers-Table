
using StorytellersTable.Core.Data;
using StorytellersTable.Map;
using StorytellersTable.Renderer;
using StorytellersTable.UiLogic;
using StorytellersTable.Utility.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace StorytellersTable.Campaign.Modes
{
    /// <summary>
    /// Encapsulates behavior while modifying the map tiles; layout coordinates, layered tile placement, and geometry.
    /// </summary>
    public class MapEditMode : ICampaignMode
    {
        #region private classes

        /// <summary>
        /// Stores data to track the state of area mode
        /// </summary>
        [Serializable]
        private class AreaEditData
        {
            public HexCoord AreaEditStart { get; set; }
            public bool startDefined; // states if `AreaPlaceStart` has been set

            public AreaEditData()
            {
                startDefined = false;
            }
        }

        #endregion

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
        public static Material confirmedMaterial;

        private readonly GameObject _uiPrefab;
        private readonly Transform _uiParentTransform;
        private readonly MapEditAction _inputMap;

        private readonly GameObject _confirmPlacementPrefab;

        [SerializeField] private GameObject _runtimeUiInstance; // UI for the map edit mode, instantiated from `_uiprefab`
        [SerializeField] private GameObject _runtimeConfirmPlacementUi;
        [SerializeField] private List<GameObject> _listRuntimeUi;   // list of runtime Ui

        private MapData ActiveMap => MapManager.Instance.ActiveMapData;
        private CampaignModeManager ModeManager => CampaignModeManager.Instance;

        /*
         * tiles that are not placed, where potential placement is visually shown.
         * there are 2 states:
         * 1) ghost tiles, based on immediate user input (unconfirmed tiles)
         * 2) confirmed tiles, based from user input and the current ghost tiles. These tiles will be placed on the map.
        */
        [SerializeField] private List<HexCoord> _confirmedTilePos;          // contains tiles to potentially place, they do not exist in the map data yet.
        [SerializeField] private List<HexCoord> _unconfirmedTilePos;
        [SerializeField] private List<HexRenderer> _confirmedTileVisuals;
        [SerializeField] private List<HexRenderer> _unconfirmedTileVisuals;
        [SerializeField] private GameObject _confirmedTileVisualsParent;    // confirmed tiles will be parented to this
        [SerializeField] private GameObject _unconfirmedTileVisualsParent;  // ghost tiles will be parented to this

        private readonly AreaEditData areaEditData;
        private readonly ModeContainer _editModes;

        public MapEditMode(GameObject uiPrefab, Transform uiParent, MapEditAction inputMap)
        {
            _uiPrefab = uiPrefab;
            _uiParentTransform = uiParent;
            _inputMap = inputMap;
            _confirmPlacementPrefab = Resources.Load<GameObject>("UI/MapEdit/CancelConfirmBtn");

            _runtimeUiInstance = null;
            _runtimeConfirmPlacementUi = null;
            _listRuntimeUi = new List<GameObject>();

            _confirmedTilePos = new List<HexCoord>();
            _unconfirmedTilePos = new List<HexCoord>();

            _unconfirmedTileVisuals = new List<HexRenderer>();
            _confirmedTileVisuals = new List<HexRenderer>();

            _unconfirmedTileVisualsParent = new GameObject("MapEdit - Unconfirmed_Tile_Visuals");
            _unconfirmedTileVisualsParent.transform.SetParent(CampaignModeManager.Instance.transform, true);
            _confirmedTileVisualsParent = new GameObject("MapEdit - Confirmed_Tile_Visuals");
            _confirmedTileVisualsParent.transform.SetParent(CampaignModeManager.Instance.transform, true);

            // Set initial materials
            placedMaterial = Singleton.Instance.defaultTileMaterial;
            ghostMaterial = Singleton.Instance.ghostTileMaterial;           // will probably change the shader of the material on hexRender instead of doing this
            confirmedMaterial = Singleton.Instance.ghostTileMaterial2;      // will probably change the shader of the material on hexRender instead of doing this

            areaEditData = new AreaEditData();
            _editModes = new ModeContainer();

            // Add callback to toggle radial, area, and draw tile placements
            _inputMap.Selection.ToggleSingle.performed += _editModes.ToggleSingleSelect;
            _inputMap.Selection.ToggleRadial.performed += _editModes.ToggleRadialSelect;
            _inputMap.Selection.ToggleArea.performed += _editModes.ToggleAreaSelect;
            _inputMap.Selection.ToggleDraw.performed += _editModes.ToggleDrawSelect;
            _inputMap.Selection.ClearSelection.performed += ClearTileSelection;

            // Add callbacks to toggle between tile/label edit, remove, and placement
            _inputMap.Edit.ToggleTileMode.performed += _editModes.ToggleTileMode;
            _inputMap.Edit.ToggleTileMode.performed += ClearTileSelection;

            _inputMap.Edit.ToggleEdit.performed += EditModeToggled;
            _inputMap.Edit.ToggleEdit.performed += _editModes.ToggleEdit;
            _inputMap.Edit.TogglePlace.performed += ClearTileSelection;
            _inputMap.Edit.TogglePlace.performed += _editModes.TogglePlace;
            _inputMap.Edit.ToggleRemove.performed += ClearTileSelection;
            _inputMap.Edit.ToggleRemove.performed += _editModes.ToggleRemove;
            // other call backs to input map...
        }

        void ICampaignMode.Enter()
        {
            // Instantiate UI if it does not exist
            if (_uiPrefab != null && _runtimeUiInstance == null)
            {
                _runtimeUiInstance = UnityEngine.Object.Instantiate(_uiPrefab, _uiParentTransform);
                _listRuntimeUi.Add(_runtimeUiInstance);
            }

            _inputMap.Enable();
        }

        void ICampaignMode.Exit()
        {
            _inputMap.Disable();            // disable input for this mode

            // clean up all runtime Ui
            foreach (GameObject obj in _listRuntimeUi)
                UnityEngine.Object.Destroy(obj);

            _runtimeUiInstance = null;
            _runtimeConfirmPlacementUi = null;
            _listRuntimeUi.Clear();

            // Clean up tiles visuals
            _DestroyUnconfirmedTiles();
            _DestoryConfirmedTiles();
        }

        void ICampaignMode.UpdateMode()
        {
            // Check if the mouse is over a UI element, if so do nothing 
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            // Destory current unconfirmed tiles so we can set new ones relative to the new mouse position
            _DestroyUnconfirmedTiles();

            // Get mouse's hex coordinate based on world position 
            HexCoord mouseHexCoord;
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

            #region Get mouse hex coord from world pos
            if (Physics.Raycast(ray, out RaycastHit hit, raycastMaxDistance, mapEditLayerMask))
            {
                mouseHexCoord = WorldToAxial(hit.point);
                //DebugOut.Log(this, $"Hit Point: {hit.point} | Mouse Axial: {mouseHexCoord}");

                // Check if there's existing tile data (non null) at that hex position of the map
                if (ActiveMap.tileDatas.TryGetValue(mouseHexCoord, out TileData tileData))
                {
                    //DebugOut.Log(this, $"Hovering over tile: {tileData.ToString()}");
                }
                // No tile exists at the specified hex coord
                else
                {
                    //WarningOut.Log(this, $"Hit registered at {mouseHexCoord}, but no matching tile key was found in the dictionary.");
                }
            }
            // If we can't get the mouse's world position do nothing.
            else
                return;
            #endregion

            // Add unconfirmed position at mouse position
            _unconfirmedTilePos.Add(mouseHexCoord);

            // Calculate unconfirmed tiles for settings: Radial, Area, and Draw.
            if (_editModes.SelectionMode == SelectModeTypes.radialSelect)
            {
                HexMath.GetHexRingArea(mouseHexCoord, ModeManager.mapEditSettings.radius, _unconfirmedTilePos);
            }
            else if (_editModes.SelectionMode == SelectModeTypes.areaSelect && areaEditData.startDefined)
            {
                HexMath.GetAreaAxial(areaEditData.AreaEditStart, mouseHexCoord, _unconfirmedTilePos);
            }
            else if (_editModes.SelectionMode == SelectModeTypes.drawSelect)
            {
                // compute additional "unconfirmed" tiles to potentially place, add it to the list, _unconfirmedTiles!
            }

            //DebugOut.Log(this, $"before purge: "+ HexCoord.ListToString(_unconfirmedTilePos));

            // remove duplicate positions
            _unconfirmedTilePos = _unconfirmedTilePos.ToHashSet().ToList();

            // Remove duiplicate hex positions that already exist in confirmed tiles
            foreach (HexCoord pos in _confirmedTilePos)
            {
                if (_unconfirmedTilePos.Contains(pos))
                    _unconfirmedTilePos.Remove(pos);
            }

            // Remove duplicate hex positions from _unconfirmedTilePos that exist on the map already (ie are placed tiles), for placement mode
            if (_editModes.IsTilePlaceOn())
            {
                foreach (var pair in ActiveMap.tileDatas)
                {
                    if (_unconfirmedTilePos.Contains(pair.Key))
                        _unconfirmedTilePos.Remove(pair.Key);
                }
            }
            else if (_editModes.IsLabelPlaceOn())
            {
                // logic...
            }
            // Remove hex positiosn from _unconfirmedTilePos that DO NOT EXIST on the map, for tile removal/edit mode
            else if (_editModes.IsTileRmvOn() || _editModes.IsTileEditOn())
            {
                List<HexCoord> filtered = new();
                foreach (var pair in ActiveMap.tileDatas)
                {
                    if (_unconfirmedTilePos.Contains(pair.Key))
                        filtered.Add(pair.Key);
                }
                // Clear the list and only add the filtered hex coords (hex coords that exist in the map).
                _unconfirmedTilePos.Clear();
                _unconfirmedTilePos.AddRange(filtered);
            }
            // Remove hex positiosn from _unconfirmedTilePos that DO NOT EXIST on the map, for label removal/edit mode
            else if (_editModes.IsTLabelRmvOn() || _editModes.IsLabelEditOn())
            {
                // logic...
            }

            //DebugOut.Log(this, $"after purge: " + HexCoord.ListToString(_unconfirmedTilePos));

            // Create ghost visual for unconfirmed tiles
            foreach (HexCoord hexCoord in _unconfirmedTilePos)
                GenerateGhostTile(hexCoord, ghostMaterial, _unconfirmedTileVisualsParent.transform, _unconfirmedTileVisuals);

            // Update confirmed tiles
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (_editModes.SelectionMode == SelectModeTypes.areaSelect)
                {
                    // Set the starting position
                    if (!areaEditData.startDefined)
                    {
                        areaEditData.AreaEditStart = mouseHexCoord;
                        areaEditData.startDefined = true;
                    }
                    // Deselect the start if clicked again
                    else if (areaEditData.startDefined && areaEditData.AreaEditStart == mouseHexCoord)
                        areaEditData.startDefined = false;
                    // Starting position selected, and left mouse was clicked again, ask to update confirmed tiles.
                    else
                    {
                        UpdateConfirmedTiles();
                        areaEditData.startDefined = false;
                    }
                }
                else
                {
                    UpdateConfirmedTiles();
                }
            }
        }

        /// <summary>
        /// Updates the repsective lists of confirmed tiles with the respective lists of unconfirmed tiles.
        /// Essentially transfering all the data from unconfirmed tile lists to confirmed tile lists counterparts.
        /// </summary>
        private void UpdateConfirmedTiles()
        {
            if (_unconfirmedTilePos.Count == 0)
                return;

            // Add the visuals and hex coords to their respective confirmed tile lists
            _confirmedTileVisuals.AddRange(_unconfirmedTileVisuals);
            _confirmedTilePos.AddRange(_unconfirmedTilePos);

            // load confimation ui
            LoadConfirmCancelUi();

            // Update hex visual
            foreach (HexRenderer hexRenderer in _confirmedTileVisuals)
            {
                // Instead of destroying and recreating visuals, we can reparent them
                hexRenderer.transform.SetParent(_confirmedTileVisualsParent.transform, true);
                // Set a new material for better visualization
                hexRenderer.SetMaterial(confirmedMaterial);
            }

            // Clear the unconfirmed tiles of references
            _unconfirmedTileVisuals.Clear();
            _unconfirmedTilePos.Clear();
        }

        /// <summary>
        /// Generate a ghost visual at a given HexCoord with a Material.
        /// </summary>
        /// <param name="hexCoord"></param>
        /// <param name="ghostMat"></param>
        /// <param name="hexVisualParent"></param>
        /// <param name="result"></param>
        private void GenerateGhostTile(HexCoord hexCoord, Material ghostMat, Transform hexVisualParent, List<HexRenderer> result)
        {
            HexRenderer ghostVisual = GenerateHexRenderer(hexCoord, ghostMat);
            ghostVisual.transform.SetParent(hexVisualParent, true);
            result.Add(ghostVisual);
        }

        /// <summary>
        /// Places all unconfirmed tiles to the active map.
        /// </summary>
        private void _PlaceConfirmedTiles()
        {
            _confirmedTilePos = _confirmedTilePos.ToHashSet().ToList();
            foreach (HexCoord tileCoord in _confirmedTilePos)
            {
                // Set `placed` material
                TileData newData = new TileData(tileCoord, GetPositionFromAxial(tileCoord).y); // ensure you include other fields...
                MapManager.Instance.AddToActiveMap(newData);
            }

            // Clean up, MapManger will generate the placed tiles' visuals
            _DestoryConfirmedTiles();

            return;
        }

        #region tile placement/destruction

        /// <summary>
        /// Places tiles onto the map from the confirmed tiles list.
        /// </summary>
        private void ConfirmTilePlacement()
        {
            _PlaceConfirmedTiles();
            CheckToDestoryConfirmUi();
        }

        /// <summary>
        /// Removes tile visuals and related data from the screen.
        /// </summary>
        private void CancelTilePlacement()
        {
            _DestoryConfirmedTiles();
            _DestroyUnconfirmedTiles();
            CheckToDestoryConfirmUi();
        }

        /// <summary>
        /// Destroys the list of unconfirmed tile visuals and positions.
        /// </summary>
        private void _DestroyUnconfirmedTiles()
        {
            foreach (HexRenderer hexRenderer in _unconfirmedTileVisuals)
            {
                if (hexRenderer != null)
                    UnityEngine.Object.Destroy(hexRenderer.gameObject);
            }
            _unconfirmedTileVisuals.Clear();
            _unconfirmedTilePos.Clear();
        }

        /// <summary>
        /// Destroys the list of confirmed tile visuals and positions.
        /// </summary>
        private void _DestoryConfirmedTiles()
        {
            DebugOut.Log(this, "Destroying confirmed tiles.");

            foreach (HexRenderer hexRenderer in _confirmedTileVisuals)
            {
                if (hexRenderer != null)
                    UnityEngine.Object.Destroy(hexRenderer.gameObject);
            }

            _confirmedTileVisuals.Clear();
            _confirmedTilePos.Clear();
        }

        #endregion

        #region Tile Visual Generation & World <-> Hex conversions

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
        /// Call back to remove tile visuals and tile data regarding `confirmedTile` lists.
        /// </summary>
        private void ClearTileSelection(InputAction.CallbackContext context)
        {
            DebugOut.Log(this, "[CALLBACK] Clearing selected tiles.");
            _DestoryConfirmedTiles();
            CheckToDestoryConfirmUi();
        }

        private void EditModeToggled(InputAction.CallbackContext context)
        {
            if (_editModes.IsEditOn())
                return;
            
            // if the mode is not set to edit already, we need to clear the coordinate selection
            ClearTileSelection(context);
        }

        // functions for Input Action call backs...

        #endregion

        #region UI backend

        /// <summary>
        /// Instantiates a gameobject from the prefab, `_confirmPlacementPrefab`, only one may exist.
        /// </summary>
        private void LoadConfirmCancelUi() // NOTE: this should only be for remove and place modes, and the proper button listeners need to be set.
        {
            if (_runtimeConfirmPlacementUi != null)
                return;

            GameObject obj = UnityEngine.Object.Instantiate(_confirmPlacementPrefab, _uiParentTransform);
            MapEditCancelConfirm ui = obj.GetComponent<MapEditCancelConfirm>();

            ui.cancelBtn.onClick.AddListener(CancelTilePlacement);
            ui.confirmBtn.onClick.AddListener(ConfirmTilePlacement);

            _runtimeConfirmPlacementUi = obj;
            _listRuntimeUi.Add(obj);
        }

        /// <summary>
        /// Checks whether or not `_confirmedTilePos` count is > 0 to destory the runtime ui `_runtimeConfirmPlacementUi`.
        /// </summary>
        private void CheckToDestoryConfirmUi()
        {
            // don't destroy the ui if there are tiles that are not placed down yet.
            if (_confirmedTilePos.Count > 0)
                return;

            DestroyConfirmUi();
        }

        /// <summary>
        /// Destory's the runtime ui `_runtimeConfirmPlacementUi`.
        /// </summary>
        private void DestroyConfirmUi()
        {
            UnityEngine.Object.Destroy(_runtimeConfirmPlacementUi);
            _runtimeConfirmPlacementUi = null;
        }

        #endregion

        #region misc: LayoutMap(), SetPlacedMaterial()
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

        #endregion
    }
}
