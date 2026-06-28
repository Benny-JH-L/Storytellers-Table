
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

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
        public float outerSize = 1f;
        public float innerSize = 0f;
        public float height = 1f;
        public bool isFlatTopped;
        public Material placedMaterial;       // material of placed tiles --> set based on UI
        public Material ghostMaterial;  // material of unconfirmed tiles --> should be grabbed in MapEditMode consstructor.

        private readonly GameObject _uiPrefab;
        private readonly Transform _uiParentTransform;
        private readonly MapEditAction _inputMap;

        [SerializeField] private GameObject _runtimeUiInstance;
        public MapBase map;   // for now drag and drop the map in the unity inspector, will need to get it from the scene manager class later

        // tiles that are not placed, where potential placement is visually shown.
        private List<GameObject> _unconfirmedTiles;

        // track different placement types (only one may be enabled at a time)
        private bool _radialOn, _areaOn, _drawOn;

        public MapEditMode(GameObject uiPrefab, Transform uiParent, MapEditAction inputMap)
        {
            _uiPrefab = uiPrefab;
            _uiParentTransform = uiParent;
            _inputMap = inputMap;

            _runtimeUiInstance = null;
            _unconfirmedTiles = new List<GameObject>();
            // get ghost material here...
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
                Debug.Log("Map edit - hi!!");

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

                // Check if there's a tile object (non null) at that hex position of the map
                if (map.graph.TryGetValue(mouseHexCoord, out GameObject hitTile))
                {
                    TileComponent tileComp = hitTile.GetComponent<TileComponent>();
                    //Debug.Log($"Hover over | existing tile: {tileComp.GetData<TileBase>().GetType().Name} at coord: {tileComp.HexCoord}");

                    // highlight the tile
                    _highlightTile(tileComp);
                }
                // No tile exists at the specified hex coord
                else
                {
                    //Debug.LogWarning($"Hit registered at {mouseHexCoord}, but no matching tile key was found in the dictionary.");

                    // Create ghost Tile Component at mouse's hex coord
                    GameObject newTile = _GenerateTile(mouseHexCoord, ghostMaterial);
                    _unconfirmedTiles.Add(newTile);
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
        /// Places all unconfirmed tiles on the active map.
        /// </summary>
        private void PlaceUnconfirmedTiles()
        {
            foreach (GameObject tile in _unconfirmedTiles)
            {
                // Set `placed` material
                TileComponent tileComp = tile.GetComponent<TileComponent>();
                tileComp.HexRenderer.SetMaterial(placedMaterial);

                // Set parent of tile and add the tile to the active map 
                tile.transform.SetParent(map.transform, true);
                map.graph[tileComp.HexCoord] = tile;
            }

            _unconfirmedTiles.Clear();  // clear the list, do not destory GameObjects
            return;
        }

        #region Tile Generation & World <-> Hex conversions
        /// <summary>
        /// Generates a hex tile at the specified hex axial, q & r, coordinate, and places it at the converted world space position, 
        /// then adds it to the map's adjacency list.
        /// </summary>
        /// <param name="hexCoord"></param>
        /// <param name="mat"></param>
        /// <returns></returns>
        private GameObject _GenerateTile(HexCoord hexCoord, Material mat)
        {
            // Create GameObject with HexRenderer and TileComponent
            GameObject tile = new GameObject($"Hex ({hexCoord.q},{hexCoord.r})", typeof(HexRenderer), typeof(TileComponent));
            tile.transform.position = _GetPositionFromAxial(hexCoord);

            // Set up HexRenderer
            HexRenderer hexRenderer = tile.GetComponent<HexRenderer>();
            hexRenderer.outerSize = outerSize;
            hexRenderer.innerSize = innerSize;
            hexRenderer.height = height;
            hexRenderer.isFlatTopped = isFlatTopped;
            hexRenderer.SetMaterial(mat);
            hexRenderer.DrawMesh();
            hexRenderer.SetHexText(hexCoord);

            // Set up and bridge the tile data context
            TileBase tileData = new TileBase(hexCoord);
            TileComponent tileComponent = tile.GetComponent<TileComponent>();
            tileComponent.Setup(hexCoord, tileData);

            return tile;
        }

        /// <summary>
        /// Generates a hex tile at the worldPos converted to specified hex axial, q & r, coordinate, and places it at the converted world space position, 
        /// then adds it to the map's adjacency list.
        /// </summary>
        /// <param name="worldPos"></param>
        /// <param name="mat"></param>
        /// <returns></returns>
        private GameObject _GenerateTile(Vector3 worldPos, Material mat)
        {
            return _GenerateTile(WorldToAxial(worldPos), mat);
        }

        /// <summary>
        /// Computes the exact 3D world position from the hex coordinate using structural basis vector matrix transformations.
        /// This removes all floating point tracking gaps and anchors the origin natively at (0,0,0).
        /// </summary>
        private Vector3 _GetPositionFromAxial(HexCoord coord)
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
        public HexCoord WorldToAxial(Vector3 worldPos)
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
        
        /// <summary>
        /// Destroys the list of unconfimed tiles GameObjets.
        /// </summary>
        private void _DestroyUnconfirmedTiles()
        {
            foreach (GameObject tile in _unconfirmedTiles)
            {
                if (tile != null)
                    Object.Destroy(tile);
            }
            _unconfirmedTiles.Clear();
        }

        private void _highlightTile(TileComponent tileComp)
        {
            // code here
        }

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
                Debug.Log("Map edit - Radial disabled");
                return;
            }

            Debug.Log("Map edit - Radial enabled");
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
                Debug.Log("Map edit - Area disabled");
                return;
            }

            Debug.Log("Map edit - Area enabled");
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
                Debug.Log("Map edit - draw disabled");
                return;
            }

            Debug.Log("Map edit - draw enabled");
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
