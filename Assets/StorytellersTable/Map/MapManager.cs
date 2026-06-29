
using StorytellersTable.Core.Data;
using StorytellersTable.Utility.Log;
using System.Collections.Generic;
using UnityEngine;

namespace StorytellersTable.Map
{
    /// <summary>
    /// Manages active map visuals, file routing, in-memory tier maps, and runtime selection pipelines.
    /// </summary>
    [DisallowMultipleComponent]
    public class MapManager : MonoBehaviour
    {
        public static MapManager Instance { get; private set; }

        [SerializeField] public Vector2Int defaultMapSize = new Vector2Int(5, 5);

        // used to store hex tile visual
        private GameObject hexRendererParent;

        private readonly static string simulatedSwitch = "1";

        public MapData ActiveMapData { get; private set; }


        /// <summary>
        /// Caches loaded map datas from disk.
        /// </summary>
        private readonly Dictionary<string, MapData> _loadedMapsCache = new Dictionary<string, MapData>();

        private void Awake()
        {
            // Destroy itself if another exists
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            hexRendererParent = new GameObject("HexRendererParent");
            hexRendererParent.transform.SetParent(this.transform, true);

            Instance = this;    // set the first instance of MapDataManager

            // initialize `_loadedMapsCache` from a save file on disk...
        }

        private void OnEnable()
        {
            // simulate switching to a map
            // set the active map data to the a world map saved on disk
            DebugOut.Log(this, "switching active map in OnEnable()");
            Instance.SwitchActiveMap(simulatedSwitch);

            if (ActiveMapData == null)
                ErrorOut.Log(this, "switched active map is `null` in OnEnable()");
        }

        /// <summary>
        /// Add a MapData to the manager's cached maps.
        /// </summary>
        /// <param name="mapData"></param>
        public void RegisterMapToCache(MapData mapData)
        {
            if (mapData == null || string.IsNullOrEmpty(mapData.mapId))
            {
                string mesg = mapData == null ? "mapData is null" : $"map id `{mapData.mapId}` is null/empty";
                ErrorOut.Log(this, "Could not register to cache: " + mesg);
                return;
            }
            _loadedMapsCache[mapData.mapId] = mapData;
        }

        /// <summary>
        /// Switches the active map data. If the id is not recognized, it will generate a new map with that id.
        /// The new map's type will be dependent on the active map's type.
        /// Ex.
        /// If the active map is `null` then it will generate a `WorldMap`.
        /// If the active map is a `WorldMap` then it will generate a `StageMap` map data.
        /// If the active map is a `FloorMap` then it will generate another `FloorMap`.
        /// </summary>
        /// <param name="targetMapId"></param>
        public void SwitchActiveMap(string targetMapId)
        {
            if (string.IsNullOrEmpty(targetMapId))
            {
                ErrorOut.Log(this, $"Cannot switch to map with id: `{targetMapId}`");
                return;
            }
            else if (ActiveMapData != null && targetMapId == ActiveMapData.mapId)
            {
                DebugOut.Log(this, $"Map id[{targetMapId}] is already active, type[{ActiveMapData.GetType()}]");
                return;
            }

            // Get the map specified by the id
            if (_loadedMapsCache.TryGetValue(targetMapId, out MapData mapData))
            {
                ActiveMapData = mapData;
                DebugOut.Log(this, $"Switched to map with id[{mapData.mapId}], type[{ActiveMapData.GetType()}]");

                // load the new map's visuals
                LoadNewMapVisuals(mapData);
                
                return;
            }

            // Clear current map visuals before making a new map
            ClearActiveMapVisuals();

            // If the map id is not in the cache, create a new map
            MapData newMap = GenerateBlankMapData(targetMapId);
            ActiveMapData = newMap;
            _loadedMapsCache.Add(targetMapId, newMap);

            // Generate generic layout
            StorytellersTable.Campaign.Modes.MapEditMode.LayoutMap(this, defaultMapSize, Singleton.Instance.defaultTileMaterial);
            DebugOut.Log(this, $"Generating a new map with id[{targetMapId}], type[{newMap.GetType()}]");
        }

        /// <summary>
        /// Add `TileData` to the active map, and generate's hex tile visual.
        /// </summary>
        /// <param name="data"></param>
        public void AddToActiveMap(TileData data)
        {
            ActiveMapData.tileDatas[data.hexCoord] = data;
            GenerateHexTileVisual(data);    // add hex tile visual
        }

        #region Map visuals: generating, clearing
        /// <summary>
        /// Loads map visuals: tiles, labels, entities, etc.
        /// </summary>
        /// <param name="mapData"></param>
        private void LoadNewMapVisuals(MapData mapData)
        {
            ClearActiveMapVisuals(); // clear the current map visuals before loading new ones

            // Generate tile visuals
            foreach (var pair in mapData.tileDatas)
                GenerateHexTileVisual(pair.Value);

            // Other visuals ...
        }

        /// <summary>
        /// Clear map visuals of the current map, labels, tiles, entities, etc,
        /// </summary>
        private void ClearActiveMapVisuals()
        {
            if (ActiveMapData == null)
                return;

            // Destroy tile visuals
            foreach (Transform child in hexRendererParent.transform)
                Destroy(child.gameObject);
            
            // other visuals ...
        }

        /// <summary>
        /// Generate a hex tile visual using TileData to the scene.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private void GenerateHexTileVisual(TileData data)
        {
            // get material based on tile data's `tileType`
            Material tileMat = Singleton.Instance.defaultTileMaterial;

            HexRenderer hexRenderer = StorytellersTable.Campaign.Modes.MapEditMode.GenerateHexRenderer(data.hexCoord, tileMat);
            hexRenderer.transform.SetParent(hexRendererParent.transform, true);    // parent it
        }
        #endregion

        #region Map Layout, Debugging, and clearing graph

        /// <summary>
        /// Rebuilds the map
        /// </summary>
        [ContextMenu("Rebuild Map")] // In the Unity inspector, right click the map script, and select this option
        public void RebuildMap()
        {
            Debug.Log($"Re building map of size q={defaultMapSize.x}, r={defaultMapSize.y}...");
            StorytellersTable.Campaign.Modes.MapEditMode.LayoutMap(this, defaultMapSize, Singleton.Instance.defaultTileMaterial);
        }

        [ContextMenu("Re Draw Hex Tile Mesh")] // In the Unity inspector, right click the map script, and select this 
        public void ReDrawTileMesh()
        {
            Debug.Log("Re drawing tile mesh...");

            foreach (Transform child in hexRendererParent.transform)
                child.gameObject.GetComponent<HexRenderer>().DrawMesh();
        }

        /// <summary>
        /// Clears and destroys all map tiles on this map
        /// </summary>
        [ContextMenu("Clear Map Tiles")]
        public void ClearTiles()
        {
            ClearActiveMapVisuals();
            ActiveMapData.Clear();
        }
        #endregion

        /// <summary>
        /// Generates a generic map based on the active map's type, generates a WorldMap if the active map is `null`.
        /// Ex. 
        /// If the active map is a `WorldMap` then it will generate a `StageMap` map data.
        /// If the active map is a `FloorMap` then it will generate another `FloorMap`.
        /// </summary>
        /// <param name="mapId"></param>
        /// <returns></returns>
        private MapData GenerateBlankMapData(string mapId)
        {
            MapData retMapData;

            // If there is no active map data (ie case of no world map)
            if (ActiveMapData is null)
                retMapData = new WorldMap();
            // If the active map is a `WorldMap`, create a `StageMap`
            else if (ActiveMapData is WorldMap)
                retMapData = new StageMap();
            // If the active map is a `StageMap` or `FloorMap`, create a `FloorMap`
            else
                retMapData = new FloorMap();

            retMapData.mapId = mapId;
            retMapData.mapName = $"`{retMapData.GetType()}`_id[{mapId}]";

            return retMapData;
        }
    }
}