using StorytellersTable.Utility.Log;
using StorytellersTable.Renderer;
using System.Collections.Generic;
using UnityEngine;
using Assets.StorytellersTable.Core.Map;

namespace StorytellersTable.Map
{
    public struct UpdateMapInfoPackage
    {
        /// <summary>
        /// Contains the updated info.
        /// </summary>
        public HashSet<TileData> info;
    }

    /// <summary>
    /// Manages active map visuals, file routing, in-memory tier maps, and runtime selection pipelines.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-500)]
    public class MapManager : MonoBehaviour
    {
        private readonly static string simulatedSwitch = "1";   // simulates map switching id

        public static MapManager Instance { get; private set; }

        [SerializeField] public Vector2Int defaultMapSize = new Vector2Int(5, 5);

        /// <summary>
        /// Caches loaded map datas from disk.
        /// </summary>
        private readonly Dictionary<string, MapData> _loadedMapsCache = new Dictionary<string, MapData>();

        // handles axial coordinate visuals of tiles in the map
        [SerializeField] public CoordinatesRenderer coordinatesRenderer;

        // handles tile visuals of the map
        [SerializeField] public MapTileRenderer mapTileRenderer;

        public MapData ActiveMapData { get; private set; }

        private void Awake()
        {
            // Destroy itself if another exists
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            DebugOut.Log(this, "Awake()");

            mapTileRenderer = new GameObject("Map Tile Renderer", typeof(MapTileRenderer)).GetComponent<MapTileRenderer>();
            mapTileRenderer.transform.SetParent(this.transform, true);

            coordinatesRenderer = new GameObject("MapManager's Coordinate Renderer", typeof(Canvas), typeof(CoordinatesRenderer)).GetComponent<CoordinatesRenderer>();
            coordinatesRenderer.transform.SetParent(this.transform, true);

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
            StorytellersTable.Campaign.Modes.MapEditorContainer.LayoutMap(this, defaultMapSize, MaterialLoader.instance.GetDefaultMaterial());
            DebugOut.Log(this, $"Generating a new map with id[{targetMapId}], type[{newMap.GetType()}]");
        }


        /// <summary>
        /// Loads map visuals: tiles, labels, entities, etc.
        /// </summary>
        /// <param name="mapData"></param>
        private void LoadNewMapVisuals(MapData mapData)
        {
            ClearActiveMapVisuals(); // clear the current map visuals before loading new ones

            foreach ((Layer layer, var value) in mapData.mapTileData.GetTileRepresentation())
            {
                foreach ((HexCoord hex, TileData data) in value)
                {
                    // Generate tile visuals
                    mapTileRenderer.AddHexTileVisual(data);

                    // Generate hex coord labels
                    coordinatesRenderer.AddLabel(data);
                }
            }    

            //foreach ((HexCoord hexCoord, TileData tileData) in mapData.tileDatas)
            //{
            //    // Generate tile visuals
            //    mapTileRenderer.AddHexTileVisual(tileData);

            //    // Generate hex coord labels
            //    coordinatesRenderer.AddLabel(tileData);
            //}

            // Other visuals ...
        }

        public void AddToActiveMap(UpdateMapInfoPackage package)
        {
            DebugOut.Log(this, "Adding tile datas...");
            foreach (TileData data in package.info)
            {
                AddToActiveMap(data);
            }
        }

        /// <summary>
        /// Add <paramref name="tileData"/> to the active map, and generate's hex tile visual and label.
        /// </summary>
        /// <param name="tileData"></param>
        public void AddToActiveMap(TileData tileData)
        {
            HexCoord hexCoord = tileData.hexCoord;
            ActiveMapData.mapTileData.AddTile(tileData);
            mapTileRenderer.AddHexTileVisual(tileData);    // add hex tile visual // TODO: add level diffing
            coordinatesRenderer.AddLabel(tileData);        // add hex pos label
        }

        public void AddToActiveMap(MapTileRepresentation other)
        {
            //ActiveMapData.mapTileData.AddFromMapTileRepresentation(other); // already done

            foreach (var dict in other.GetTileRepresentation().Values)
            {
                foreach (TileData tileData in dict.Values)
                    AddToActiveMap(tileData);
            }
        }

        /// <summary>
        /// Removes all items in <paramref name="datas"/> from the active map, their visual, coordinate label, and tile data.
        /// </summary>
        /// <param name="datas"></param>
        public void RemoveFromActiveMap(UpdateMapInfoPackage package)
        {
            //DebugOut.Log(this, "Removing tile datas...");
            foreach (TileData data in package.info)
            {
                RemoveFromActiveMap(data);
            }
        }

        /// <summary>
        /// Removes tile with <paramref name="hexCoord"/> from the active map, its visual, coordinate label, and tile data.
        /// </summary>
        /// <param name="hexCoord"></param>
        public void RemoveFromActiveMap(TileData tileData)
        {
            //DebugOut.Log(this, $"Removing [{hexCoord}] tile");

            ErrorOut.Log(this, "RemoveFromActiveMap NOT FULLY IMPLEMENTED");
            // remove the position label
            //coordinatesRenderer.RemoveLabel(ActiveMapData.tileDatas[hexCoord]); 

            //// Destroy tile visual
            //mapTileRenderer.RemoveVisual(hexCoord);

            // remove position & tile data from the map data
            //ActiveMapData.tileDatas.Remove(hexCoord);
            ActiveMapData.mapTileData.TryRemove(tileData);
        }

        /// <summary>
        /// Set new tile data based on the HexCoord in <paramref name="newData"/>.
        /// </summary>
        /// <param name="newData"></param>
        public void SetNewTileData(TileData newData)
        {
            ErrorOut.Log(this, "SetNewTileData NOT FULLY IMPLEMENTED");
            HexCoord hexCoord = newData.hexCoord;

            // Update the coordinate renderer
            //coordinatesRenderer.RemoveLabel(ActiveMapData.tileDatas[hexCoord]);
            //coordinatesRenderer.AddLabel(ActiveMapData.tileDatas[hexCoord]);

            // Set the new data
            //ActiveMapData.tileDatas[hexCoord] = newData;
            ActiveMapData.mapTileData.UpdateTile(newData);
        }

        /// <summary>
        /// Clear map visuals of the current map, labels, tiles, entities, etc,
        /// </summary>
        private void ClearActiveMapVisuals()
        {
            if (ActiveMapData == null)
                return;

            // Destroy tile visuals
            mapTileRenderer.ClearVisuals();

            // Destroy hex position labels
            coordinatesRenderer.ClearLabels();

            // other visuals ...
        }

        #region Context Menu: Map Layout, Debugging

        /// <summary>
        /// Rebuilds the map
        /// </summary>
        [ContextMenu("Rebuild Map")] // In the Unity inspector, right click the map script, and select this option
        public void RebuildMap()
        {
            Debug.Log($"Re building map of size q={defaultMapSize.x}, r={defaultMapSize.y}...");
            StorytellersTable.Campaign.Modes.MapEditorContainer.LayoutMap(this, defaultMapSize, MaterialLoader.instance.GetDefaultMaterial());
        }

        [ContextMenu("Re Draw Hex Tile Mesh")] // In the Unity inspector, right click the map script, and select this 
        public void ReDrawTileMesh()
        {
            Debug.Log("Re drawing tile mesh...");

            mapTileRenderer.ReDrawMesh();
            coordinatesRenderer.ClearLabels();

            // rebuild labels
            //foreach (var pair in ActiveMapData.tileDatas)
            //    coordinatesRenderer.AddLabel(pair.key);
        }

        /// <summary>
        /// Clears and destroys all map tiles on this map
        /// </summary>
        [ContextMenu("Clear Map Tiles")]
        public void ClearTiles()
        {
            ClearActiveMapVisuals();
            ActiveMapData.mapTileData.Clear();
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