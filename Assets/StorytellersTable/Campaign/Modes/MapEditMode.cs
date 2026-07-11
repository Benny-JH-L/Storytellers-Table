
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
        public static float height = 1f;

        // material of placed tiles --> set based on UI
        public static Material placedMaterial;       
        public static Material ghostMaterial;
        public static Material confirmedMaterial;

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
        [SerializeField] private List<HexCoord> _confirmedTilePos;          // contains tiles to potentially place, they do not exist in the map data yet.
        [SerializeField] private List<HexCoord> _unconfirmedTilePos;
        [SerializeField] private List<HexRenderer> _confirmedPosVisuals;
        [SerializeField] private List<HexRenderer> _unconfirmedPosVisuals;
        [SerializeField] private GameObject _confirmedPosVisualsParent;    // confirmed positions will be parented to this
        [SerializeField] private GameObject _unconfirmedPosVisualsParent;  // ghost tiles will be parented to this

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

            _unconfirmedPosVisuals = new List<HexRenderer>();
            _confirmedPosVisuals = new List<HexRenderer>();

            _unconfirmedPosVisualsParent = new GameObject("MapEdit - Unconfirmed_Pos_Visuals");
            _unconfirmedPosVisualsParent.transform.SetParent(CampaignModeManager.Instance.transform, true);
            _confirmedPosVisualsParent = new GameObject("MapEdit - Confirmed_Pos_Visuals");
            _confirmedPosVisualsParent.transform.SetParent(CampaignModeManager.Instance.transform, true);

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
            _unconfirmedTilePos.Add(mouseHexCoord);

            // Calculate unconfirmed positions for settings: Radial, Area, and Draw.
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

            // remove duplicate positions
            _unconfirmedTilePos = _unconfirmedTilePos.ToHashSet().ToList();

            // Remove duiplicate hex positions that already exist in confirmed positions list
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
            // Remove hex positiosn from _unconfirmedTilePos that DO NOT EXIST on the map, FOR TILE removal/edit mode
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
            // Remove hex positiosn from _unconfirmedTilePos that DO NOT EXIST on the map, FOR LABEL removal/edit mode
            else if (_editModes.IsTLabelRmvOn() || _editModes.IsLabelEditOn())
            {
                // logic...
            }

            // Create ghost visual for unconfirmed tiles
            foreach (HexCoord hexCoord in _unconfirmedTilePos)
                GenerateGhostTile(hexCoord, ghostMaterial, _unconfirmedPosVisualsParent.transform, _unconfirmedPosVisuals);

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
            _confirmedPosVisuals.AddRange(_unconfirmedPosVisuals);
            _confirmedTilePos.AddRange(_unconfirmedTilePos);

            // load confimation ui for placement/removal modes
            if (_editModes.IsPlacementOn() || _editModes.IsRemoveOn())
                LoadConfirmCancelUi();
            // else load Ui for tile/label editing

            // Update hex visual
            foreach (HexRenderer hexRenderer in _confirmedPosVisuals)
            {
                // Instead of destroying and recreating visuals, we can reparent them
                hexRenderer.transform.SetParent(_confirmedPosVisualsParent.transform, true);
                // Set a new material for better visualization
                hexRenderer.SetMaterial(confirmedMaterial);
            }

            // Clear the unconfirmed tiles of references
            _unconfirmedPosVisuals.Clear();
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
            _confirmedTilePos = _confirmedTilePos.ToHashSet().ToList(); // ensure there are no duplicate hex positions
            foreach (HexCoord tileCoord in _confirmedTilePos)
            {
                // Set `placed` material
                TileData newData = new TileData(tileCoord, HexMath.GetPositionFromAxial(tileCoord).y); // ensure you include other fields...
                MapManager.Instance.AddToActiveMap(newData);
            }

            // Clean up, MapManger will generate the placed tiles' visuals
            _DestoryConfirmedTiles();

            return;
        }

        #region tile placement/destruction

        /// <summary>
        /// Places tiles onto the map from the confirmed positions list.
        /// </summary>
        private void ConfirmTilePlacement()
        {
            _PlaceConfirmedTiles();
            CheckToDestoryConfirmUi();
        }

        /// <summary>
        /// Removes tile visuals and related data from the confirmed positions list.
        /// </summary>
        private void ClearConfirmedPositions()
        {
            _DestoryConfirmedTiles();
            CheckToDestoryConfirmUi();
        }

        /// <summary>
        /// Removes tile data and visual from the active map using `_confirmedTilePos`.
        /// </summary>
        private void RmvConfirmedPosFromActiveMap()
        {
            List<TileData> tileDatas = new List<TileData>();

            // get the tile datas with the hex coordinates in the list
            foreach (HexCoord tileCoord in _confirmedTilePos)
                tileDatas.Add(MapManager.Instance.ActiveMapData.tileDatas[tileCoord]);

            MapManager.Instance.RemoveFromActiveMap(tileDatas);
            ClearConfirmedPositions();
        }

        /// <summary>
        /// Destroys the list of unconfirmed tile visuals and positions.
        /// </summary>
        private void _DestroyUnconfirmedTiles()
        {
            foreach (HexRenderer hexRenderer in _unconfirmedPosVisuals)
            {
                if (hexRenderer != null)
                    UnityEngine.Object.Destroy(hexRenderer.gameObject);
            }
            _unconfirmedPosVisuals.Clear();
            _unconfirmedTilePos.Clear();
        }

        /// <summary>
        /// Destroys the list of confirmed tile visuals and positions.
        /// </summary>
        private void _DestoryConfirmedTiles()
        {
            DebugOut.Log(this, "Destroying confirmed tiles.");

            foreach (HexRenderer hexRenderer in _confirmedPosVisuals)
            {
                if (hexRenderer != null)
                    UnityEngine.Object.Destroy(hexRenderer.gameObject);
            }

            _confirmedPosVisuals.Clear();
            _confirmedTilePos.Clear();
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
            //hexRenderer.isFlatTopped = Singleton.Instance.isFlatTopped;
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
