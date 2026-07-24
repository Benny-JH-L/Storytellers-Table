
using StorytellersTable.Core.Data;
using StorytellersTable.Map;
using StorytellersTable.Renderer;
using StorytellersTable.UiLogic;
using StorytellersTable.Utility.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace StorytellersTable.Campaign.Modes
{
    /// <summary>
    /// Encapsulates behavior while modifying the map tiles; layout coordinates, layered tile placement, and geometry.
    /// </summary>
    [DisallowMultipleComponent]
    public class MapEditorContainer : MonoBehaviour, ICampaignMode
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

        public static MapEditorContainer instance;

        // Raycast to this layer to place tiles
        public static LayerMask mapEditLayerMask;
        public static float raycastMaxDistance = 500f;

        //private readonly GameObject _uiPrefab;
        private Transform _uiParentTransform => Singleton.Instance.mainCanvas;
        [SerializeField] private MapEditAction _inputMap;

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

        private readonly AreaEditData areaEditData = new AreaEditData();
        public readonly ModeContainer editModes = new ModeContainer();
        private bool selectOn = true;  // selection/deselction state

        // UI
        [SerializeField] MapEditorUIManager mapEditorUI;

        // values edit by the UI
        public string placementMaterialName  = MaterialLoader.instance.defaultMaterialName;
        public static int height_placement = 1;   // for placement mode

        private TileEditContainer TileEditMode => TileEditContainer.instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;

            mapEditLayerMask = LayerMask.GetMask("MapEditPlane");
            _inputMap = new MapEditAction();
        }

        private void OnEnable()
        {
            confirmedPosVisuals = new GameObject("MapEdit - Confirmed_Pos_Visuals", typeof(MapTileRenderer)).GetComponent<MapTileRenderer>();
            confirmedPosVisuals.transform.SetParent(CampaignModeManager.Instance.transform, true);

            ghostMapRenderer = new GameObject("MapEdit - Ghost_Visuals", typeof(MapTileRenderer)).GetComponent<MapTileRenderer>();
            ghostMapRenderer.transform.SetParent(CampaignModeManager.Instance.transform, true);

            // Add callback to toggle radial, area, and draw tile placements
            _inputMap.Selection.ToggleSingle.performed += editModes.ToggleSingleSelect;
            _inputMap.Selection.ToggleRadial.performed += editModes.ToggleRadialSelect;
            _inputMap.Selection.ToggleArea.performed += editModes.ToggleAreaSelect;
            _inputMap.Selection.ToggleDraw.performed += editModes.ToggleDrawSelect;
            _inputMap.Selection.ToggleDeselect.performed += ToggleSelect;
            _inputMap.Selection.ClearSelection.performed += ClearConfirmedPositions;

            // Add callbacks to toggle between tile/label edit, remove, and placement
            _inputMap.Edit.ToggleTileMode.performed += editModes.ToggleTileMode;
            _inputMap.Edit.ToggleTileMode.performed += ClearConfirmedPositions;

            _inputMap.Edit.ToggleEdit.performed += editModes.ToggleEdit;
            //_inputMap.Edit.ToggleEdit.performed += EditModeChanged;

            _inputMap.Edit.TogglePlace.performed += editModes.TogglePlace;
            //_inputMap.Edit.TogglePlace.performed += EditModeChanged;

            _inputMap.Edit.ToggleRemove.performed += editModes.ToggleRemove;
            //_inputMap.Edit.ToggleRemove.performed += EditModeChanged;
            // other call backs to input map...
        }

        void ICampaignMode.Enter()
        {
            // Create the MapEditors UI
            mapEditorUI = Instantiate(Singleton.Instance.mapEditorUIPrefab, Singleton.Instance.mainCanvas).GetComponent<MapEditorUIManager>();
            // Note: the constant destruction and creation can lag down the game once the UI becomes much more developed

            // Enable Edit mode by default
            editModes.ToggleEdit();
            TileEditMode.Activate();

            _inputMap.Enable();
        }

        void ICampaignMode.Exit()
        {
            _inputMap.Disable();    // disable input for this mode

            TileEditMode.Disable();

            // Note: the constant destruction and creation can lag down the game once the UI becomes much more developed
            Destroy(mapEditorUI.gameObject);

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
            //ClearUnconfirmedTiles(); // causes issues with the rising/falling animation for hex visuals, it resetting the `rise start time`
            ghostMapRenderer.ClearVisuals();    // Clear ghost visuals
            unconfirmedHexCoords.Clear();       // Clear ghost coord info

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
            if (editModes.SelectionMode == SelectModeTypes.radialSelect)
            {
                HexMath.GetHexRingArea(mouseHexCoord, ModeManager.mapEditSettings.radius, unconfirmedHexCoords);
            }
            // Area
            else if (editModes.SelectionMode == SelectModeTypes.areaSelect && areaEditData.startDefined)
            {
                HexMath.GetAreaAxial(areaEditData.AreaEditStart, mouseHexCoord, unconfirmedHexCoords);
            }
            // Draw (for radius)
            else if (editModes.SelectionMode == SelectModeTypes.drawSelect)
            {
                HexMath.GetHexRingArea(mouseHexCoord, ModeManager.mapEditSettings.drawRadius - 1, unconfirmedHexCoords);    // offset draw radius by 1, so draw radius of 1 means single tile, 2 means 1 tiles from the center.
            }

            // remove duplicate positions
            unconfirmedHexCoords = unconfirmedHexCoords.ToHashSet().ToList();

            // Handle Select / Deselect states (and highlight states)
            if (selectOn)
                HandleSelectState();
            else
                HandleDeselectState();

            // Update values for tile editing mode
            if (TileEditMode.IsActive)
                TileEditMode.SetValues(confirmedHexCoords.ToHashSet());

            // Update confirmed tiles
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                // Area mode check
                if (editModes.SelectionMode == SelectModeTypes.areaSelect)
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
            else if (editModes.SelectionMode == SelectModeTypes.drawSelect && Mouse.current.leftButton.isPressed)
                UpdateConfirmedTiles();
        }

        #region handling select & deselect state
        /// <summary>
        /// In the select state, updates confirmed list, or unconfirmed list, based on Selection Mode: Radial, Area, Single, or Draw selection.
        /// </summary>
        /// <remarks>
        /// For PlacementMode, updates ghost visuals. For Edit and Removal modes, updates the higlight state of unconfirmed tiles.
        /// </remarks>
        private void HandleSelectState()
        {
            // Remove duiplicate hex positions that already exist in confirmed positions list
            foreach (HexCoord pos in confirmedHexCoords)
            {
                if (unconfirmedHexCoords.Contains(pos))
                    unconfirmedHexCoords.Remove(pos);
            }

            // Remove duplicate hex positions from unconfirmedHexCoords that exist on the map already (ie are placed tiles), for placement mode
            if (editModes.IsTilePlaceOn())
            {
                foreach (var pair in ActiveMap.tileDatas)
                {
                    if (unconfirmedHexCoords.Contains(pair.Key))
                        unconfirmedHexCoords.Remove(pair.Key);
                }
            }
            else if (editModes.IsLabelPlaceOn())
            {
                // logic...
            }
            // Remove hex positiosn from unconfirmedHexCoords that DO NOT EXIST on the map, FOR TILE removal/edit mode
            else if (editModes.IsTileRmvOn() || editModes.IsTileEditOn())
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
            else if (editModes.IsTLabelRmvOn() || editModes.IsLabelEditOn())
            {
                // logic...
            }

            // Set visuals for unconfirmed tiles
            // Placement mode, create ghost visual for unconfirmed tiles 
            if (editModes.IsPlacementOn())
            {
                ghostMapRenderer.AddHexTileVisual(unconfirmedHexCoords, placementMaterialName, height_placement);
                ghostMapRenderer.EnableGhostVisual(unconfirmedHexCoords, true);
            }
            // Edit or removal mode, highlight existing tiles
            else
            {
                // Disable highlight of tiles not in the unconfirmed list
                MapManager.Instance.mapTileRenderer.DisableAllHighlightsExcept(unconfirmedHexCoords.ToHashSet());
                // Enable highlight of tiles
                MapManager.Instance.mapTileRenderer.EnableHighlight(unconfirmedHexCoords, true);
            }
        }

        /// <summary>
        /// In the deselect state, updates visuals. Ie, highlights confirmed tiles that exist in the unconfirmed list for deselection.
        /// </summary>
        private void HandleDeselectState()
        {
            // Placement mode, highlight hex coords to deselect (ie remove from confirmed list)
            if (editModes.IsTilePlaceOn())
            {
                // Disable highlight of tiles not in the unconfirmed list
                confirmedPosVisuals.DisableAllHighlightsExcept(unconfirmedHexCoords.ToHashSet());

                // highlights ghost tiles the mouse is over (based on selection mode)
                confirmedPosVisuals.EnableHighlight(unconfirmedHexCoords, true);
            }
            // Edit and remove mode, highlight selected hex coords on the map to unselect
            else if (confirmedHexCoords.Count > 0)
            {
                MapManager.Instance.mapTileRenderer.DisableAllHighlightsExcept(unconfirmedHexCoords.ToHashSet());

                // Set highlight for coords that the unconfirmed list intersects with the confirmed list
                HashSet<HexCoord> tmpHashSet = confirmedHexCoords.ToHashSet();
                foreach (HexCoord hexCoord in unconfirmedHexCoords)
                {
                    // only highlight the hex coords if unconfirmed items intersects with the confirmed list
                    if (tmpHashSet.Contains(hexCoord))
                        MapManager.Instance.mapTileRenderer.EnableHighlight(hexCoord, true);
                }
            }
        }

        #endregion

        /// <summary>
        /// Updates confirmed list with the unconfirmed list. Visuals for confirmed tiles are updated.
        /// </summary>
        /// <remarks>
        /// Unconfirmed list (and if applicable, its associated ghost visuals) are cleared.
        /// </remarks>
        private void UpdateConfirmedTiles()
        {
            if (unconfirmedHexCoords.Count == 0)
                //if ((ghostMapRenderer.Count() == 0 && _editModes.IsPlacementOn()) || unconfirmedHexCoords.Count == 0)
                //if (_unconfirmedTilePos.Count == 0)
                return;

            //confirmedPosVisuals.DisableAllHighlights(); // reset highlight visuals

            if (selectOn) // Select
            {
                // In PlacementMode, create new confirmed visuals from ghostMapRenderer
                if (editModes.IsPlacementOn())
                {
                    foreach ((HexCoord coord, HexRenderer hexRenderer) in ghostMapRenderer.GetVisualData())
                    {
                        confirmedPosVisuals.AddHexTileVisual(coord, placementMaterialName, hexRenderer.height);
                        confirmedPosVisuals.EnableGhostVisual(coord, true);
                    }
                }
                // For edit and removal modes, set the selected visual state to true
                else
                {
                    MapManager.Instance.mapTileRenderer.EnableSelectedVisual(unconfirmedHexCoords, true);
                }

                confirmedHexCoords.AddRange(unconfirmedHexCoords);
            }
            else // Deselect
            {
                // Placement mode, remove visual at hex coords from unconfirmed list
                if (editModes.IsPlacementOn())
                {
                    confirmedPosVisuals.RemoveVisual(unconfirmedHexCoords);
                }
                // For edit and removal modes, set the selected visual state to false
                else
                {
                    MapManager.Instance.mapTileRenderer.EnableSelectedVisual(unconfirmedHexCoords, false);
                }

                // Remove coords from confirmed list, update the confirmed list
                HashSet<HexCoord> tmpHashSet = confirmedHexCoords.ToHashSet();

                foreach (HexCoord coord in unconfirmedHexCoords)
                    tmpHashSet.Remove(coord);

                confirmedHexCoords.Clear();
                confirmedHexCoords.AddRange(tmpHashSet.ToList());
            }

            // check if we need to create or destroy UI.
            if (confirmedHexCoords.Count > 0)
            {
                // load confimation ui for placement/removal modes
                if (editModes.IsPlacementOn() || editModes.IsRemoveOn())
                    LoadConfirmCancelUi();
                // load Ui for tile / label editing
                else
                {
                    // NOTE: done in another class!
                }
            }
            else
                CheckToDestoryConfirmUi();

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
                TileData newData = new TileData(tileCoord, HexMath.GetPositionFromAxial(tileCoord).y, height_placement, placementMaterialName);
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
            MapManager.Instance.mapTileRenderer.DisableAllHighlights();
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
        public static HexRenderer GenerateHexRenderer(HexCoord hexCoord, Material mat, int height)
        {
            HexRenderer hexRenderer = new GameObject($"Hex ({hexCoord.q},{hexCoord.r})", typeof(HexRenderer)).GetComponent<HexRenderer>();
            // Set up where the visual's position in the world
            hexRenderer.transform.position = HexMath.GetPositionFromAxial(hexCoord);

            // Set up HexRenderer
            hexRenderer.outerSize = Singleton.Instance.outerSize;
            hexRenderer.innerSize = Singleton.Instance.innerSize;
            hexRenderer.height = height;
            hexRenderer.SetSharedMaterial(mat);
            hexRenderer.DrawMesh();

            return hexRenderer;
        }

        public static HexRenderer GenerateHexRenderer(Vector3 worldPos, Material mat, int height)
        {
            return GenerateHexRenderer(HexMath.WorldToAxial(worldPos), mat, height);
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

        /// <summary>
        /// When the edit mode changes, this is called.
        /// </summary>
        /// <remarks>
        /// Called by ModeContainer instances, whenever the mode is changed.
        /// </remarks>
        public void EditModeChanged()
        {
            editModes.PrintEditModeHistory();
            Stack<EditModeTypes> history = editModes.GetEditModeHistory();

            // Case, when it is initially one/no history
            if (history.Count < 2)
                return;

            history.Pop(); // gets the current mode
            EditModeTypes prevEditMode = history.Pop();

            DebugOut.Log(this, $"prevMode: {prevEditMode}, currMode: {editModes.EditMode}");

            // Check if the Editing mode is enabled already
            if (prevEditMode == editModes.EditMode && editModes.IsEditingOn())
                return;

            // Switch to editing mode if same mode is selected
            if (prevEditMode == editModes.EditMode || editModes.IsEditingOn())
            {
                TileEditMode.Activate();
            }
            // No longer in EditMode, disable it
            else
            {
                TileEditMode.Disable();
            }

            ClearUnconfirmedTiles();
            ClearConfirmedPositions();
        }

        /// <summary>
        /// Toggles selection, (either select on or deselect on).
        /// </summary>
        /// <param name="context"></param>
        private void ToggleSelect(InputAction.CallbackContext context)
        {
            selectOn = !selectOn;
            DebugOut.Log(this, "Selection: " + selectOn + " (False means deselect is on)");
            ClearUnconfirmedTiles();
        }
        // functions for Input Action call backs...

        #endregion

        #region UI backend

        /// <summary>
        /// Loads the "Cancel" or "Confirm" UI, and assigns the proper button listeners.
        /// SHOULD ONLY CALLED FOR PLACEMENT/REMOVAL MODES.
        /// </summary>
        private void LoadConfirmCancelUi() // NOTE: this should only be for remove and place modes, and the proper button listeners need to be set.
        {
            if (editModes.IsPlacementOn())
            {
                mapEditorUI.LoadConfirmCancelUI(ClearConfirmedPositions, ConfirmTilePlacement);
            }
            else if (editModes.IsRemoveOn())
            {
                mapEditorUI.LoadConfirmCancelUI(ClearConfirmedPositions, RmvConfirmedPosFromActiveMap);
            }
        }

        /// <summary>
        /// Checks whether or not `_confirmedTilePos` count is > 0 to destory the "Cancel" or "Confirm" UI.
        /// </summary>
        private void CheckToDestoryConfirmUi()
        {
            // don't destroy the ui if there are "confirmed tiles" chosen.
            if (confirmedPosVisuals.Count() > 0)
                return;

            mapEditorUI.DestroyConfirmCancelUI();
        }
        #endregion

        #region misc: LayoutMap()

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
            DebugOut.Log(typeof(MapEditorContainer), $"LayoutMap() - elapsed time: {sw.Elapsed.TotalSeconds} seconds.");
        }

        #endregion
    }
}
