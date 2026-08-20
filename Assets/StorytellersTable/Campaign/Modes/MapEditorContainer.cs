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
using Unity.VisualScripting;
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
        private Transform _uiParentTransform => Singleton.Instance.mainCanvas;
        [SerializeField] private MapEditAction _inputMap;

        private MapData ActiveMapData => MapManager.Instance.ActiveMapData;
        private CampaignModeManager ModeManager => CampaignModeManager.Instance;

        private readonly AreaEditData areaEditData = new AreaEditData();
        public readonly ModeContainer editModes = new ModeContainer();
        private bool selectOn = true;  // selection/deselction state

        [SerializeField] MapEditorUIManager mapEditorUI;    // map editor's own UI

        private TileEditContainer TileEditMode => TileEditContainer.instance;   // handles UI for tile data editing by the user
        private SelectionContainer selectionContainer;          // handles tile selection unconfirmed & confirmed tracking, and relavent visual states.
        private TemporaryTileContainer temporaryTileContainer;  // utilized by placement mode
        private HexGridSelector gridSelector = new();


        // values edit by the UI
        public string placementMaterialName = MaterialLoader.instance.defaultMaterialName;

        [SerializeField] public int activeLayer = 0; // when this changes i need to also move the GameObject named "Plane (for mapedit raycasting)" to the same y-pos for better feedback!
                                                     // ^ chages need to be made: keybinds to move camerea up/down to `space bar` and `left-ctrl` respectively, (use left-alt for cam up/down thing) then use q/e to move the active layer down and up respectively
                                                     // i might want to redo the placement, edit, removal logic with the new placement class.
        [SerializeField] public int layerRange = 2;

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

            // Add callback to toggle radial, area, and draw tile placements
            _inputMap.Selection.ToggleSingle.performed += editModes.ToggleSingleSelect;
            _inputMap.Selection.ToggleRadial.performed += editModes.ToggleRadialSelect;
            _inputMap.Selection.ToggleArea.performed += editModes.ToggleAreaSelect;
            _inputMap.Selection.ToggleDraw.performed += editModes.ToggleDrawSelect;
            _inputMap.Selection.ToggleDeselect.performed += ToggleSelect;
            _inputMap.Selection.ClearSelection.performed += ClearConfirmedSelection;
            
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

            _inputMap.Enable();
        }

        void ICampaignMode.Exit()
        {
            _inputMap.Disable();    // disable input for this mode

            TileEditMode.Disable();

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

            HexCoord mouseHexCoord;

            #region Get the mouse hex coord from world pos via plane raycast

            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            var plane = new Plane(Vector3.up, new Vector3(0f, activeLayer, 0f));    // create a plane based on the `activeLayer`

            if (!plane.Raycast(ray, out float dist))                                // ray cast to this plane to find the mouse position in world
                return;
            
            mouseHexCoord = HexMath.WorldToAxial(ray.GetPoint(dist));               // convert the world position found to hex coord
            #endregion

            // Add unconfirmed position at mouse position
            HashSet<HexCoord> unconfirmedHexCoords = new() { mouseHexCoord };
            MapTileRendererPackage unconfirmedTilePackage = new MapTileRendererPackage() { info = new() };

            // TODO: thinking of redoing how hexcoords are chosen for radial, area, and draw -> make it similar to single select and using the grid selector, or have a toggle, whether or not to toggle the `fall through` (with a range) or just only on the desired layer

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
            else if (editModes.SelectionMode == SelectModeTypes.singleSelect)
            {
                if (gridSelector.TryPick(activeLayer, out Layer outLayer, out HexCoord outHexCoord, out _))
                {
                    unconfirmedTilePackage.info[outLayer] = new() { outHexCoord };

                    //DebugOut.Log(this, "out: " + outHexCoord + " outlayer: " + outLayer);
                }
            }

            //GridSelectorPayload gridSelectorPay = new GridSelectorPayload() { coords = unconfirmedHexCoords, activeLayer = new Layer(activeLayer), layerRange = layerRange };
            //gridSelector.PickWithCamera(gridSelectorPay, Camera.main, out List<TileData> tileDataPicked);

            // filter out hexcoords based on the active edit mode
            //DebugOut.Log(this, "before filter:");
            //Printer.Print(unconfirmedTilePackage.info);

            unconfirmedTilePackage.info[new Layer(activeLayer)] = unconfirmedHexCoords;
            FilterOutHexcoords(unconfirmedTilePackage);

            //DebugOut.Log(this, "after filter:");
            //Printer.Print(unconfirmedTilePackage.info);

            // create packages to update visuals & values
            CreatePackage(unconfirmedTilePackage, out UpdateMapInfoPackage mapInfoPackage);

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
                    selectionContainer.RemoveFromConfirmed(mapInfoPackage);
                    return;
                }

                // Check for tile placement
                if (editModes.IsTilePlaceOn())
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
                            PlaceTmpTiles();
                            areaEditData.startDefined = false;
                        }
                    }
                    else
                    {
                        PlaceTmpTiles();
                    }
                }
                else
                {
                    selectionContainer.UpdateConfirmed();
                }
            }
            #endregion
        }

        /// <summary>
        /// Filters the information in <paramref name="package"/> based on the current active modes.
        /// </summary>
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
                        package.info.Add(new TileData(hexCoord, activeLayer, placementMaterialName));
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

        #region Input Action Callbacks

        /// <summary>
        /// Callback to clear the confirmed tile selection.
        /// </summary>
        /// <param name="context"></param>
        private void ClearConfirmedSelection(InputAction.CallbackContext context)
        {
            ClearConfirmedSelection();
        }

        /// <summary>
        /// Toggles selection, (either select on or deselect on).
        /// </summary>
        /// <param name="context"></param>
        private void ToggleSelect(InputAction.CallbackContext context)
        {
            selectOn = !selectOn;
            DebugOut.Log(this, "Selection: " + selectOn + " (False means deselect is on)");
        }
        // functions for Input Action call backs...

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
            HexRenderer hexRenderer = new GameObject($"Hex ({hexCoord.q},{hexCoord.r})", typeof(HexRenderer)).GetComponent<HexRenderer>();
            // Set up where the visual's position in the world
            Vector3 pos = HexMath.GetPositionFromAxial(hexCoord);
            pos.y += layer.Y(); // offset by layer
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
