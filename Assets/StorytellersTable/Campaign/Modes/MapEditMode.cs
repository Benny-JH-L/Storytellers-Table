
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

        private readonly GameObject _uiPrefab;
        private readonly Transform _uiParentTransform;
        private readonly MapEditAction _inputMap;

        private readonly GameObject _confirmPlacementPrefab;    // ui

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
        private List<HexCoord> unconfirmedHexCoords = new List<HexCoord>(); // used by edit, and removal modes
        private List<HexCoord> confirmedHexCoords = new List<HexCoord>();   // used by edit, and removal modes
        [SerializeField] private MapTileRenderer confirmedPosVisuals;    // used by placement mode, contains tiles to potentially place, they do not exist in the map data yet.
        [SerializeField] private MapTileRenderer ghostMapRenderer;       // used by placement mode, contains ghost tiles, they do not exist in the map data.

        private readonly AreaEditData areaEditData;
        private readonly ModeContainer _editModes;

        // values edit by the UI
        private static string tileTypeId = "Sweet :)";
        private static int height = 1;
        private Material selectedMaterial;          // material of tiles

        public MapEditMode(GameObject uiPrefab, Transform uiParent, MapEditAction inputMap)
        {
            _uiPrefab = uiPrefab;
            _uiParentTransform = uiParent;
            _inputMap = inputMap;
            _confirmPlacementPrefab = Resources.Load<GameObject>("UI/MapEdit/CancelConfirmBtn");

            _runtimeUiInstance = null;
            _runtimeConfirmPlacementUi = null;
            _listRuntimeUi = new List<GameObject>();

            confirmedPosVisuals = new GameObject("MapEdit - Confirmed_Pos_Visuals", typeof(MapTileRenderer)).GetComponent<MapTileRenderer>();
            confirmedPosVisuals.transform.SetParent(CampaignModeManager.Instance.transform, true);

            ghostMapRenderer = new GameObject("MapEdit - Ghost_Visuals", typeof(MapTileRenderer)).GetComponent<MapTileRenderer>();
            ghostMapRenderer.transform.SetParent(CampaignModeManager.Instance.transform, true);

            // Set initial material
            selectedMaterial = Singleton.Instance.defaultTileMaterial;

            areaEditData = new AreaEditData();
            _editModes = new ModeContainer();

            // Add callback to toggle radial, area, and draw tile placements
            _inputMap.Selection.ToggleSingle.performed += _editModes.ToggleSingleSelect;
            _inputMap.Selection.ToggleRadial.performed += _editModes.ToggleRadialSelect;
            _inputMap.Selection.ToggleArea.performed += _editModes.ToggleAreaSelect;
            _inputMap.Selection.ToggleDraw.performed += _editModes.ToggleDrawSelect;
            _inputMap.Selection.ClearSelection.performed += ClearConfirmedPositions;

            // Add callbacks to toggle between tile/label edit, remove, and placement
            _inputMap.Edit.ToggleTileMode.performed += _editModes.ToggleTileMode;
            _inputMap.Edit.ToggleTileMode.performed += ClearConfirmedPositions;

            _inputMap.Edit.ToggleEdit.performed += EditModeToggled;
            _inputMap.Edit.ToggleEdit.performed += _editModes.ToggleEdit;
            _inputMap.Edit.TogglePlace.performed += ClearConfirmedPositions;
            _inputMap.Edit.TogglePlace.performed += _editModes.TogglePlace;
            _inputMap.Edit.ToggleRemove.performed += ClearConfirmedPositions;
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
            _inputMap.Disable();    // disable input for this mode

            // clean up all runtime Ui
            foreach (GameObject obj in _listRuntimeUi)
                UnityEngine.Object.Destroy(obj);

            _runtimeUiInstance = null;
            _runtimeConfirmPlacementUi = null;
            _listRuntimeUi.Clear();

            // Clean up tiles visuals
            ClearUnconfirmedTiles();
            ClearConfirmedTiles();
        }

        void ICampaignMode.UpdateMode()
        {
            // Check if the mouse is over a UI element, if so do nothing 
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            // Destory current unconfirmed tiles so we can set new ones relative to the new mouse position
            ClearUnconfirmedTiles();

            // Get mouse's hex coordinate based on world position 
            HexCoord mouseHexCoord;
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

            #region Get mouse hex coord from world pos
            if (Physics.Raycast(ray, out RaycastHit hit, raycastMaxDistance, mapEditLayerMask))
            {
                mouseHexCoord = HexMath.WorldToAxial(hit.point);
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
            unconfirmedHexCoords.Add(mouseHexCoord);

            // Calculate unconfirmed positions for settings: Radial, Area, and Draw.
            // Radial
            if (_editModes.SelectionMode == SelectModeTypes.radialSelect)
            {
                HexMath.GetHexRingArea(mouseHexCoord, ModeManager.mapEditSettings.radius, unconfirmedHexCoords);
            }
            // Area
            else if (_editModes.SelectionMode == SelectModeTypes.areaSelect && areaEditData.startDefined)
            {
                HexMath.GetAreaAxial(areaEditData.AreaEditStart, mouseHexCoord, unconfirmedHexCoords);
            }
            // Draw (for radius)
            else if (_editModes.SelectionMode == SelectModeTypes.drawSelect)
            {
                HexMath.GetHexRingArea(mouseHexCoord, ModeManager.mapEditSettings.drawRadius, unconfirmedHexCoords);
            }

            // remove duplicate positions
            unconfirmedHexCoords = unconfirmedHexCoords.ToHashSet().ToList();

            // Remove duiplicate hex positions that already exist in confirmed positions list
            foreach (HexCoord pos in confirmedHexCoords)
            {
                if (unconfirmedHexCoords.Contains(pos))
                    unconfirmedHexCoords.Remove(pos);
            }

            // Remove duplicate hex positions from unconfirmedHexCoords that exist on the map already (ie are placed tiles), for placement mode
            if (_editModes.IsTilePlaceOn())
            {
                foreach (var pair in ActiveMap.tileDatas)
                {
                    if (unconfirmedHexCoords.Contains(pair.Key))
                        unconfirmedHexCoords.Remove(pair.Key);
                }
            }
            else if (_editModes.IsLabelPlaceOn())
            {
                // logic...
            }
            // Remove hex positiosn from unconfirmedHexCoords that DO NOT EXIST on the map, FOR TILE removal/edit mode
            else if (_editModes.IsTileRmvOn() || _editModes.IsTileEditOn())
            {
                List<HexCoord> filtered = new();
                foreach (var pair in ActiveMap.tileDatas)
                {
                    if (unconfirmedHexCoords.Contains(pair.Key))
                        filtered.Add(pair.Key);
                }
                // Clear the list and only add the filtered hex coords (hex coords that exist in the map).
                unconfirmedHexCoords.Clear();
                unconfirmedHexCoords.AddRange(filtered);
            }
            // Remove hex positiosn from unconfirmedHexCoords that DO NOT EXIST on the map, FOR LABEL removal/edit mode
            else if (_editModes.IsTLabelRmvOn() || _editModes.IsLabelEditOn())
            {
                // logic...
            }

            // Set visuals for unconfirmed tiles
            // Placement mode, create ghost visual for unconfirmed tiles 
            if (_editModes.IsPlacementOn())
            {
                ghostMapRenderer.AddHexTileVisual(unconfirmedHexCoords, selectedMaterial);
                ghostMapRenderer.SetGhostVisual(unconfirmedHexCoords, true);
            }
            // Edit or removal mode, highlight existing tiles
            else
            {
                MapManager.Instance.mapTileRenderer.SetHighlight(unconfirmedHexCoords, true);
            }

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
            // Update confirmed tiles for draw selection
            else if (_editModes.SelectionMode == SelectModeTypes.drawSelect && Mouse.current.leftButton.isPressed)
                UpdateConfirmedTiles();
        }

        /// <summary>
        /// Updates list of confirmed tiles with the list of unconfirmed tiles. Visuals for confirmed tiles are updated.
        /// </summary>
        /// <remarks>
        /// Unconfirmed tiles (and if applicable, its associated ghost visuals) are cleared.
        /// </remarks>
        private void UpdateConfirmedTiles()
        {
            //DebugOut.Log(this, "unconfirmed count: " + unconfirmedHexCoords.Count);
            if ((ghostMapRenderer.Count() == 0 && _editModes.IsPlacementOn()) || unconfirmedHexCoords.Count == 0)
                //if (_unconfirmedTilePos.Count == 0)
                return;

            // load confimation ui for placement/removal modes
            if (_editModes.IsPlacementOn() || _editModes.IsRemoveOn())
                LoadConfirmCancelUi();
            // load Ui for tile / label editing
            else
            {
                
            }

            // In PlacementMode, create new confirmed visuals from ghostMapRenderer
            if (_editModes.IsPlacementOn())
            {
                foreach ((HexCoord coord, HexRenderer hexRenderer) in ghostMapRenderer.GetVisualData())
                {
                    confirmedPosVisuals.AddHexTileVisual(coord, selectedMaterial);
                    confirmedPosVisuals.SetGhostVisual(coord, true);
                }
            }
            // For edit and removal modes, set the selected visual state
            else
            {
                MapManager.Instance.mapTileRenderer.SetSelectedVisual(unconfirmedHexCoords, true);
            }

            confirmedHexCoords.AddRange(unconfirmedHexCoords);
            ClearUnconfirmedTiles();    // will also handle clearing ghostMap visuals
        }

        /// <summary>
        /// Places all confirmed tiles to the active map.
        /// </summary>
        private void PlaceConfirmedTiles()
        {
            foreach ((HexCoord tileCoord, _) in confirmedPosVisuals.GetVisualData())    // use the MapRenderer data and not the list
            {
                // Set `placed` material
                TileData newData = new TileData(tileCoord, HexMath.GetPositionFromAxial(tileCoord).y, height, tileTypeId);
                MapManager.Instance.AddToActiveMap(newData);
            }

            // Clean up, MapManger will generate the placed tiles' visuals
            ClearConfirmedTiles();
        }

        #region tile placement/destruction & tile removeal from map

        /// <summary>
        /// Places tiles onto the map from the confirmed positions list.
        /// </summary>
        private void ConfirmTilePlacement()
        {
            PlaceConfirmedTiles();
            CheckToDestoryConfirmUi();
        }

        /// <summary>
        /// Removes tile visuals and related data from the confirmed positions list.
        /// </summary>
        private void ClearConfirmedPositions()
        {
            ClearConfirmedTiles();
            CheckToDestoryConfirmUi();
        }

        /// <summary>
        /// Clears the visuals of unconfirmed tiles; If in PlacementMoode, clears temporary visuals, otherwise disables tile highlight visual state.
        /// Clears unconfirmed hex coordinates list.
        /// </summary>
        /// <remarks>
        /// If in PlacementMode (tile or label), it will clear ghost tile visuals. Otherwise, tiles highlights will be disabled.
        /// </remarks>
        private void ClearUnconfirmedTiles()
        {
            //DebugOut.Log(this, "Destroying unconfirmed tiles...");

            // Clear ghost visuals
            ghostMapRenderer.ClearVisuals();
            // Disable tile highlight
            MapManager.Instance.mapTileRenderer.DisableAllHighlights();
            unconfirmedHexCoords.Clear();
        }

        /// <summary>
        /// Clears the visuals of confirmed tiles; If in PlacementMode, clears temporary visuals, otherwise disables tile selected visual state. 
        /// Clears the confirmed hex coordinate list.
        /// </summary>
        private void ClearConfirmedTiles()
        {
            //DebugOut.Log(this, "Destroying confirmed tiles...");

            confirmedPosVisuals.ClearVisuals();
            MapManager.Instance.mapTileRenderer.DisableAllSelectedVisuals();
            confirmedHexCoords.Clear();
        }

        /// <summary>
        /// Removes tile data and visual from the active map using `confirmedHexCoords` data.
        /// </summary>
        private void RmvConfirmedPosFromActiveMap()
        {
            MapManager.Instance.RemoveFromActiveMap(confirmedHexCoords);
            ClearConfirmedPositions();
        }

        #endregion

        #region hex visual generation
        public static HexRenderer GenerateHexRenderer(HexCoord hexCoord, Material mat)
        {
            HexRenderer hexRenderer = new GameObject($"Hex ({hexCoord.q},{hexCoord.r})", typeof(HexRenderer)).GetComponent<HexRenderer>();
            // Set up where the visual's position in the world
            hexRenderer.transform.position = HexMath.GetPositionFromAxial(hexCoord);

            // Set up HexRenderer
            hexRenderer.outerSize = Singleton.Instance.outerSize;
            hexRenderer.innerSize = Singleton.Instance.innerSize;
            hexRenderer.height = height;
            hexRenderer.SetMaterial(mat);
            hexRenderer.DrawMesh();

            return hexRenderer;
        }

        public static HexRenderer GenerateHexRenderer(Vector3 worldPos, Material mat)
        {
            return GenerateHexRenderer(HexMath.WorldToAxial(worldPos), mat);
        }
        #endregion

        #region Input Action Callbacks

        /// <summary>
        /// Callback to remove tile visuals and tile data regarding confirmed position lists.
        /// </summary>
        private void ClearConfirmedPositions(InputAction.CallbackContext context)
        {
            ClearConfirmedPositions();
        }

        private void EditModeToggled(InputAction.CallbackContext context)
        {
            if (_editModes.IsEditOn())
                return;
            
            // if the mode is not set to edit already, we need to clear the coordinate selection
            ClearConfirmedPositions(context);
        }

        // functions for Input Action call backs...

        #endregion

        #region UI backend

        /// <summary>
        /// Instantiates a gameobject from the prefab, `_confirmPlacementPrefab`, only one may exist.
        /// SHOULD ONLY CALLED FOR PLACEMENT/REMOVAL MODES.
        /// </summary>
        private void LoadConfirmCancelUi() // NOTE: this should only be for remove and place modes, and the proper button listeners need to be set.
        {
            if (_runtimeConfirmPlacementUi != null)
                return;

            GameObject obj = UnityEngine.Object.Instantiate(_confirmPlacementPrefab, _uiParentTransform);
            MapEditCancelConfirm ui = obj.GetComponent<MapEditCancelConfirm>();

            if (_editModes.IsPlacementOn())
            {
                ui.cancelBtn.onClick.AddListener(ClearConfirmedPositions);
                ui.confirmBtn.onClick.AddListener(ConfirmTilePlacement);
            }
            else if (_editModes.IsRemoveOn())
            {
                ui.cancelBtn.onClick.AddListener(ClearConfirmedPositions);
                ui.confirmBtn.onClick.AddListener(RmvConfirmedPosFromActiveMap);
            }


            _runtimeConfirmPlacementUi = obj;
            _listRuntimeUi.Add(obj);
        }

        /// <summary>
        /// Checks whether or not `_confirmedTilePos` count is > 0 to destory the runtime ui `_runtimeConfirmPlacementUi`.
        /// </summary>
        private void CheckToDestoryConfirmUi()
        {
            // don't destroy the ui if there are "confirmed tiles" chosen.
            if (confirmedPosVisuals.Count() > 0)
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
            selectedMaterial = newMat;
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
                    if (Singleton.Instance.isFlatTopped)
                    {
                        int qFlat = r;
                        int offsetFlat = Mathf.FloorToInt(qFlat / 2f);
                        int rFlat = q;
                        hexCoord = new HexCoord(qFlat, rFlat + offsetFlat);
                    }

                    // Generate tile data, then add it to the map. 
                    TileData newData = new TileData(hexCoord, HexMath.GetPositionFromAxial(hexCoord).y); // ENSURE YOU ADD THE OTHER DETAILS!
                    mapManager.AddToActiveMap(newData);
                }
            }

            sw.Stop();  // stop timer
            DebugOut.Log(typeof(MapEditMode), $"LayoutMap() - elapsed time: {sw.Elapsed.TotalSeconds} seconds.");
        }

        #endregion
    }
}
