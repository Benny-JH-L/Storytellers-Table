using Assets.StorytellersTable.Core.Map;
using StorytellersTable.Map;
using StorytellersTable.Renderer;
using StorytellersTable.UiLogic;
using StorytellersTable.Utility.Log;
using StorytellersTable.Utility.Printer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
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
        public static MapEditorContainer instance;
        [SerializeField] private MapEditAction _inputMap;   // keybinds
        private MapData ActiveMapData => MapManager.Instance.ActiveMapData;
        private CampaignModeManager ModeManager => CampaignModeManager.Instance;
        private Transform _uiParentTransform => Singleton.Instance.mainCanvas;


        private HexGridSelector gridSelector = new();           // handles map grid coordinate selection
        private SelectionContainer selectionContainer;          // handles tile selection unconfirmed & confirmed tracking, and relavent visual states.
        private TemporaryTileContainer temporaryTileContainer;  // utilized by placement mode
        private TileEditContainer TileEditMode => TileEditContainer.instance;   // handles UI for tile data editing by the user
        public readonly ModeContainer editModes = new ModeContainer();
        //private readonly AreaEditData areaEditData = new AreaEditData();
        private AreaSelectPayload areaSelectPayload;
        private AreaSelectionContainer areaSelectionContainer;
        
        [SerializeField] MapEditorUIManager mapEditorUI;    // map editor's own UI


        #region values edit by the UI/keybinds ----
        public string placementMaterialId = MaterialLoader.instance.defaultMaterialName;

        [SerializeField] public int activeLayer = 0; // chages need to be made: keybinds to move camerea up/down to `space bar` and `left-ctrl` respectively, (use caps-lock for cam up/down thing) then use q/e to move the active layer down and up respectively
        [Range(0, 5)]
        [SerializeField] public uint layerRange = 2;
        [SerializeField] public bool layerFocusOn = false;      // toggle to focus editing/placeing on a specific layer (overrides surfaceFocusOn)
        [SerializeField] public bool surfaceFocusOn = false;    // toggle to focus selecting the surface for edit/remove (this is always on for dynamic tile placement) -> deprecate
        [SerializeField] private bool selectOn = true;          // selection/deselction state

        #endregion values edit by the UI end/keybinds ----


        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            _inputMap = new MapEditAction();
        }

        private void OnEnable()
        {
            selectionContainer = new GameObject("SelectionContainer", typeof(SelectionContainer)).GetComponent<SelectionContainer>();
            selectionContainer.transform.SetParent(this.transform, false);

            temporaryTileContainer = new GameObject("TmpTileContainer", typeof(TemporaryTileContainer)).GetComponent<TemporaryTileContainer>();
            temporaryTileContainer.transform.SetParent(this.transform, false);

            areaSelectionContainer = new(ActiveMapData.mapTileData);

            // Add callback to toggle radial, area, and draw tile placements
            _inputMap.Selection.ToggleSingle.performed += editModes.ToggleSingleSelect;
            _inputMap.Selection.ToggleSingle.performed += ResetAreaSelect;
            _inputMap.Selection.ToggleRadial.performed += editModes.ToggleRadialSelect;
            _inputMap.Selection.ToggleRadial.performed += ResetAreaSelect;
            _inputMap.Selection.ToggleArea.performed += editModes.ToggleAreaSelect;
            _inputMap.Selection.ToggleDraw.performed += editModes.ToggleDrawSelect;
            _inputMap.Selection.ToggleDraw.performed += ResetAreaSelect;
            _inputMap.Selection.ToggleDeselect.performed += ToggleSelect;
            _inputMap.Selection.ClearSelection.performed += ClearConfirmedSelection;
            _inputMap.Selection.ClearSelection.performed += ResetAreaSelect;


            // Add callbacks to toggle between tile/label edit, remove, and placement
            _inputMap.Edit.ToggleTileMode.performed += editModes.ToggleTileMode;
            _inputMap.Edit.ToggleTileMode.performed += ClearConfirmedSelection;
            _inputMap.Edit.ToggleEdit.performed += editModes.ToggleEdit;
            _inputMap.Edit.TogglePlace.performed += editModes.TogglePlace;
            _inputMap.Edit.ToggleRemove.performed += editModes.ToggleRemove;
            // other call backs to input map...
        }

        void ICampaignMode.Enter()
        {
            // Create the MapEditors UI
            mapEditorUI = Instantiate(Singleton.Instance.mapEditorUIPrefab, Singleton.Instance.mainCanvas).GetComponent<MapEditorUIManager>();
            // Note: the constant destruction and creation can lag down the game once the UI becomes much more developed

            gridSelector.RebuildLevelOrder();

            // Enable Edit mode by default
            editModes.ToggleEdit();
            TileEditMode.Activate();

            // Container start up
            selectionContainer.Init();
            temporaryTileContainer.Clear();

            ResetAreaSelect();

            _inputMap.Enable();
        }

        void ICampaignMode.Exit()
        {
            _inputMap.Disable();    // disable input for this mode
            TileEditMode.Disable();
            ResetAreaSelect();

            // Note: the constant destruction and creation can lag down the game once the UI becomes much more developed
            Destroy(mapEditorUI.gameObject);

            // Clean up containers
            selectionContainer.ClearUnconfirmed();
            selectionContainer.ClearConfirmed();
            temporaryTileContainer.Clear();

        }

        void ICampaignMode.UpdateMode()
        {
            // Check if the mouse is over a UI element if so do nothing and return
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            // Destory current unconfirmed tiles so we can set new ones relative to the new mouse position
            temporaryTileContainer.Clear();

            // return if in the deselect state for tile placement
            if (!selectOn && editModes.IsTilePlaceOn())
                return;

            // Create the grid selector's payload
            GridSelectorPayload gridSelecPayload = new() {
                mode = editModes.SelectionMode == SelectModeTypes.areaSelect ? SelectModeTypes.singleSelect : editModes.SelectionMode,  // area selection will be handleed with single select + another technique
                layerRange = layerRange,
                radius = (editModes.SelectionMode == SelectModeTypes.radialSelect) ? ModeManager.mapEditSettings.radius : ModeManager.mapEditSettings.drawRadius - 1,
                mapTileRepresentation = ActiveMapData.mapTileData,
                filterWithMapRep = true,
                //areaSelectPayload = areaSelectPayload
                areaSelctContainer = areaSelectionContainer
            };

            // Results of gridselector
            HashSet <TileData> tileDataResult;
            HashSet<HexCoord> coordResult;
            GridSelectorPayload updatedPayload = GridSelectorPayload.Copy(gridSelecPayload);

            // pick on a specific layer
            if (layerFocusOn)
            {
                #region Get the mouse hex coord from world pos via plane raycast

                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                var plane = new Plane(Vector3.up, new Vector3(0f, activeLayer, 0f));    // create a plane based on the `activeLayer`

                if (!plane.Raycast(ray, out float dist))                                // raycast to this plane to find the mouse position in world
                    return;
                #endregion

                // convert the world position found to hex coord
                gridSelecPayload.initialHexCoord = HexMath.WorldToAxial(ray.GetPoint(dist));
                gridSelecPayload.initialLayer = new Layer(activeLayer);

                // placing tiles in unoccupied spaces on a fixed layer, don't filter out results with map data
                gridSelecPayload.filterWithMapRep = editModes.IsTilePlaceOn() ? false : true;

                if (gridSelector.Pick(gridSelecPayload, out tileDataResult, out coordResult))
                {
                    // Generate tiles for placement
                    if (editModes.IsTilePlaceOn())
                    {
                        tileDataResult.Clear();
                        var tileRep = gridSelecPayload.mapTileRepresentation;
                        Layer focusLayer = gridSelecPayload.initialLayer;

                        //Printer.Print(coordResult, "pick result:");   // debug
                        // Generate tiles
                        foreach (HexCoord hexCoord in coordResult)
                        {
                            // don't generate tile if one exists at the map's position already
                            if (tileRep.EntryExists(focusLayer, hexCoord))
                                continue;

                            // add generated tile
                            tileDataResult.Add(new TileData(hexCoord, focusLayer, placementMaterialId));
                        }
                        //Printer.Print(tileDataResult, "placement: ");  // debug
                    }
                }
            }
            // pick with camera (dynamic raycast)
            else
            {
                if (gridSelector.PickWithCamera(gridSelecPayload, Camera.main, out tileDataResult, out coordResult, out updatedPayload))
                {
                    // Get surface tiles
                    if (surfaceFocusOn || editModes.IsTilePlaceOn())
                    {
                        // We don't care what the grid selector chose as we will use `coordResult` to get the 'top most' tiles
                        tileDataResult.Clear();

                        var tileRep = updatedPayload.mapTileRepresentation;
                        int layerMax = updatedPayload.initialLayer.Val + (int)updatedPayload.layerRange;
                        int layerMin = updatedPayload.initialLayer.Val - (int)updatedPayload.layerRange;

                        // Get tile data based on HexGridSelector output
                        foreach (HexCoord hexCoord in coordResult)
                        {
                            tileRep.GetTileDataStack(hexCoord, out List<TileData> tileDatas, layerMax, layerMin);

                            // the first element will be the top most tile betwen the min and max
                            if (tileDatas.Count > 0)
                                tileDataResult.Add(tileDatas[0]);
                        }
                    }

                    // Update the TileData for placement mode so that new tiles are placed atop of existing ones
                    if (editModes.IsTilePlaceOn())
                    {
                        HashSet<TileData> tmp = new();
                        foreach (TileData tileData in tileDataResult)
                            tmp.Add(new TileData(tileData.hexCoord, new Layer(tileData.mapLayer.Val + 1), placementMaterialId));

                        // Add the new results
                        tileDataResult.Clear();         // clear tmp data
                        tileDataResult.UnionWith(tmp);  // add the actual data
                    }
                }
            }

            // the tile the mouse is hovering over
            TileData currentTile = tileDataResult.Count > 0 ? tileDataResult.ToList()[0] : null;

            // set an end for real time visual update, for area selection
            if (editModes.IsAreaOn() && areaSelectionContainer.Start is not null && currentTile is not null)
            {
                areaSelectionContainer.SetEnd(currentTile, layerFocusOn, out tileDataResult); // set a `end` to update visuals, then override the tiles we want to render
            }

            // Create payloads from gridselector
            UpdateMapInfoPackage mapInfoPackage = new UpdateMapInfoPackage(){ info = tileDataResult };


            #region handle select/deselect states
            // Handle select state
            if (selectOn)
            {
                if (editModes.IsTilePlaceOn())
                {
                    temporaryTileContainer.Add(mapInfoPackage); // Update tile placement state
                }
                // removal and edit modes
                else
                {
                    selectionContainer.AddUnconfirmed(mapInfoPackage);
                }
            }
            else
            {
                // return if in the deselect state for tile placement
                if (editModes.IsTilePlaceOn())
                    return;

                // Filtering for removal/editing, filter out the tiles that do not exist in the confirmed tiles (want to show selection for selected tiles)
                tileDataResult.RemoveWhere(data => !selectionContainer.ConfirmedTiles.EntryExists(data));

                selectionContainer.AddUnconfirmed(mapInfoPackage);
            }
            #endregion

            // Clear unconfirmed tiles exepct the ones in the package (so animation timers don't reset)
            selectionContainer.ClearUnconfirmedExcept(mapInfoPackage);

            #region handle mouse input
            // Update based on mouse input
            if (editModes.SelectionMode == SelectModeTypes.drawSelect && Mouse.current.leftButton.isPressed)
            {
                // Handle deselect state
                if (!selectOn)
                {
                    selectionContainer.RemoveFromConfirmed(mapInfoPackage);
                    return;
                }

                // Place tiles for draw selection
                if (editModes.IsTilePlaceOn())
                    PlaceTmpTiles();
                else
                {
                    selectionContainer.UpdateConfirmed();
                }
            }
            else if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (!selectOn)
                {
                    if (editModes.SelectionMode != SelectModeTypes.areaSelect)
                    {
                        selectionContainer.RemoveFromConfirmed(mapInfoPackage);
                        return;
                    }
                    // only confirm deselection once an end was selected
                    else if (areaSelectionContainer.End is not null)
                    {
                        areaSelectionContainer.SetEnd(currentTile, layerFocusOn, out HashSet<TileData> areaResult);
                        mapInfoPackage.info = areaResult;
                        selectionContainer.RemoveFromConfirmed(mapInfoPackage);
                        ResetAreaSelect();
                        return;
                    }
                }

                // Two-Click Area Selection Handling
                if (editModes.IsAreaOn())
                {
                    // Click 1: Starting coord
                    if (areaSelectionContainer.Start is null && currentTile is not null)
                    {
                        areaSelectionContainer.SetStart(currentTile);
                        //DebugOut.Log(this, $"area start: {areaSelectionContainer.Start}, start layer: {areaSelectionContainer.Start.mapLayer}");
                    }
                    // Click 2 (On Start Point): Cancel selection
                    else if (areaSelectionContainer.Start == areaSelectionContainer.End)
                    {
                        ResetAreaSelect();
                        //DebugOut.Log(this, "area reset");
                    }
                    // Click 2 (On Different Point): Confirm area bounds
                    else if (currentTile is not null)
                    {
                        areaSelectionContainer.SetEnd(currentTile, layerFocusOn, out HashSet<TileData> areaResult);
                        //DebugOut.Log(this, $"area start: {areaSelectionContainer.Start}, end: {areaSelectionContainer.End}\nstart layer: {areaSelectionContainer.Start.mapLayer}, end layer: {areaSelectionContainer.End.mapLayer}");

                        if (editModes.IsTilePlaceOn())
                            PlaceTmpTiles();
                        else
                            selectionContainer.UpdateConfirmed();

                        ResetAreaSelect();
                    }
                }
                else
                {
                    if (editModes.IsTilePlaceOn())
                        PlaceTmpTiles();
                    else
                        selectionContainer.UpdateConfirmed();
                }
            }
            #endregion
        }

        /// <summary>
        /// Filters the information in <paramref name="package"/> based on the current active modes.
        /// </summary>
        /// <remarks>
        /// [DEPRECATED]
        /// </remarks>
        /// <param name="package"></param>
        private void FilterOutHexcoords(MapTileRendererPackage package)
        {
            MapTileRepresentation activeMapTileRep = ActiveMapData.mapTileData;
            Dictionary<Layer, HashSet<HexCoord>> packageInfo = package.info;

            if (selectOn)
            {
                // filter out coords that already exist on the active map for placement mode
                if (editModes.IsTilePlaceOn())
                {
                    // Go though each layer
                    foreach ((var layer, HashSet<HexCoord> hashSet) in packageInfo)
                    {
                        hashSet.RemoveWhere(hexCoord => activeMapTileRep.EntryExists(layer, hexCoord));
                    }
                }
                // only contain coords that exist on the map (for removal and edit modes)
                else
                {
                    // Go though each layer
                    foreach ((var layer, HashSet<HexCoord> hashSet) in packageInfo)
                    {
                        // exclude coords that aren't on the map
                        hashSet.RemoveWhere(hexCoord => !activeMapTileRep.EntryExists(layer, hexCoord));
                        // exclude coords that are already confirmed
                        hashSet.RemoveWhere(hexCoord => selectionContainer.ConfirmedTiles.EntryExists(layer, hexCoord));
                    }
                }
            }
            else
            {
                // when deselect and placement states are on, we want to do nothing.
                if (editModes.IsPlacementOn())
                {
                    packageInfo.Clear();
                    return;
                }
                // filter out coords that aren't in the container's confirmed for deselecting (edit/remove modes)
                foreach ((var layer, HashSet<HexCoord> hashSet) in packageInfo)
                {
                    // exclude coords that weren't selected beforehand
                    hashSet.RemoveWhere(HexCoord => !selectionContainer.ConfirmedTiles.EntryExists(layer, HexCoord));
                }
            }

            // clean up empty entries
            foreach (var layer in packageInfo.Keys.ToList())
            {
                if (packageInfo[layer].Count == 0)
                    packageInfo.Remove(layer);
            }
        }


        /// <summary>
        /// Generates a <paramref name="package"/> from <paramref name="mapTileRendererPackage"/>. Uses the active map data to get TileData, if an entry does not exist, 
        /// a new TileData at the `activeLayer` and hexcoord will be created.
        /// </summary>
        /// 
        /// <remarks>
        /// [DEPRECATED]
        /// </remarks>
        /// <param name="mapTileRendererPackage"></param>
        /// <param name="package"></param>
        private void CreatePackage(MapTileRendererPackage mapTileRendererPackage, out UpdateMapInfoPackage package)
        {
            package = new() { info = new() };
            var activeTileData = ActiveMapData.mapTileData;

            foreach ((var layer, HashSet<HexCoord> set) in mapTileRendererPackage.info)
            {
                foreach (HexCoord hexCoord in set)
                {
                    if (activeTileData.EntryExists(layer, hexCoord))
                        package.info.Add(activeTileData.GetTileRepresentation()[layer][hexCoord]);
                    else
                        package.info.Add(new TileData(hexCoord, activeLayer, placementMaterialId));
                }                
            }
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

            ResetAreaSelect();

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

            // clear confirmed selction when switching from edit/remove modes to placement
            if (editModes.IsTilePlaceOn() && prevEditMode != editModes.EditMode)
            {
                ClearConfirmedSelection();
            }
        }

        /// <summary>
        /// Places tiles onto the map that's stored in `temporaryTileContainer`.
        /// </summary>
        private void PlaceTmpTiles()
        {
            MapManager.Instance.AddToActiveMap(temporaryTileContainer.TmpTiles);
            temporaryTileContainer.Clear();
            gridSelector.RebuildLevelOrder();
        }

        /// <summary>
        /// Clear's the confirmed tile selection.
        /// </summary>
        private void ClearConfirmedSelection()
        {
            selectionContainer.ClearConfirmed();
        }

        /// <summary>
        /// Callback to clear the confirmed tile selection.
        /// </summary>
        /// <param name="context"></param>
        private void ClearConfirmedSelection(InputAction.CallbackContext context)
        {
            ClearConfirmedSelection();
        }

        private void ResetAreaSelect()
        {
            //areaSelectPayload = new AreaSelectPayload();
            areaSelectionContainer.Reset();
        }

        private void ResetAreaSelect(InputAction.CallbackContext context)
        {
            ResetAreaSelect();
        }

        /// <summary>
        /// Toggles selection, (either select on or deselect on).
        /// </summary>
        /// <param name="context"></param>
        private void ToggleSelect(InputAction.CallbackContext context)
        {
            selectOn = !selectOn;
            ResetAreaSelect();
            DebugOut.Log(this, "Selection: " + selectOn + " (False means deselect is on)");
        }


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
                    TileData newData = new TileData(hexCoord, 0, MaterialLoader.instance.defaultMaterialName);
                    mapManager.AddToActiveMap(newData);
                }
            }

            sw.Stop();  // stop timer
            DebugOut.Log(typeof(MapEditorContainer), $"LayoutMap() - elapsed time: {sw.Elapsed.TotalSeconds} seconds.");
        }

        #endregion

        #region hex visual generation
        public static HexRenderer GenerateHexRenderer(HexCoord hexCoord, Material mat, Layer layer)
        {
            HexRenderer hexRenderer = new GameObject($"Hex ({hexCoord.q},{hexCoord.r}) L{layer}", typeof(HexRenderer)).GetComponent<HexRenderer>();
            // Set up where the visual's position in the world
            Vector3 pos = HexMath.GetPositionFromAxial(hexCoord);
            pos.y += layer.Y() - (Singleton.Instance.height / 2f); // offset by layer. ensure the tile surface is along the layer
            hexRenderer.transform.position = pos;

            // Set up HexRenderer
            hexRenderer.outerSize = Singleton.Instance.outerSize;
            hexRenderer.innerSize = Singleton.Instance.innerSize;
            hexRenderer.height = Singleton.Instance.height;
            hexRenderer.SetSharedMaterial(mat);
            hexRenderer.DrawMesh();

            return hexRenderer;
        }
        #endregion

    }
}
