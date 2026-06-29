
using System.Collections.Generic;
using UnityEngine;
using StorytellersTable.Utility;
using StorytellersTable.Core.Data;

namespace StorytellersTable.Map
{
    /// <summary>
    /// Manages file routing, in-memory tier maps, and runtime selection pipelines.
    /// </summary>
    [DisallowMultipleComponent]
    public class MapDataManager : MonoBehaviour
    {
        public static MapDataManager Instance { get; private set; }

        public MapData ActiveMapData { get; private set; }

        [SerializeField] public Vector2Int defaultMapSize = new Vector2Int(5, 5);

        /// <summary>
        /// Caches loaded map datas from disk.
        /// </summary>
        private readonly Dictionary<string, MapData> _loadedMapsCache = new Dictionary<string, MapData>();

        private void Awake()
        {
            // Destroy itself if another MapDataManager exists
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;    // set the first instance of MapDataManager

            // initialize `_loadedMapsCache` from a save file...
        }

        private void OnEnable()
        {
            // set the active map data to the a world map saved on disk
            Debug.LogWarning("switching active map in awake()");
            Instance.SwitchActiveMap("simulated world map load from disk");

            if (ActiveMapData == null)
                Debug.LogWarning("weojfghbewrgbfiwebguew");
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
                ErrorOut.Throw(this, "Could not register to cache: " + mesg);
                return;
            }
            _loadedMapsCache[mapData.mapId] = mapData;
        }

        public void SwitchActiveMap(string targetMapId)
        {
            if (string.IsNullOrEmpty(targetMapId))
            {
                ErrorOut.Throw(this, $"Cannot switch to map with id: `{targetMapId}`");
                return;
            }

            // Get the map specified by the id
            if (_loadedMapsCache.TryGetValue(targetMapId, out MapData mapData))
            {
                ActiveMapData = mapData;
                // other stuff if needed...
                return;
            }

            // If the map id is not in the cache, create a new map
            MapData newMap = GenerateBlankMapData(targetMapId);
            ActiveMapData = newMap;
            _loadedMapsCache.Add(targetMapId, newMap);

            // Generate generic layout
            StorytellersTable.Campaign.Modes.MapEditMode.LayoutMap(newMap, defaultMapSize);
        }

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
