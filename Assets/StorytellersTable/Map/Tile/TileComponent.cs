
using System;
using UnityEngine;

namespace StorytellersTable.Core.Data
{
    [RequireComponent(typeof(HexRenderer))]
    public class TileComponent : MonoBehaviour
    {
        [SerializeReference] public TileData _tileData;         // holds data for WorldTile, StageTile, FloorTile

        public HexCoord HexCoord { get; private set; }          // getter, setter, and variable for `HexCoord`
        public HexRenderer HexRenderer { get; private set; }    // getter, setter, and variable for `HexRenderer`

        private void Awake()
        {
            HexRenderer = GetComponent<HexRenderer>();
        }

        /// <summary>
        /// Initializes the visual tile component with its data context and coordinates.
        /// </summary>
        public void Setup(HexCoord hexCoord, TileData tileData)
        {
            HexCoord = hexCoord;
            _tileData = tileData;
        }
    }

    /// <summary>
    /// Raw tile information.
    /// </summary>
    [Serializable]
    public class TileData
    {
        public HexCoord hexCoord;
        public int elevation;
        public string tileTypeId;   // ex. Grass, Water, Snow, Desert, etc..

        /// <summary>
        /// Universal Pointer ID referencing nested lower-tier maps.
        /// If this tile exists on a WorldMap, this field references a StageMap ID.
        /// If on a StageMap, it points to a FloorMap ID.
        /// </summary>
        public string targetNestedMapId;

        public TileData(HexCoord hexCoord, int elevation = 0, string tileTypeId = "Grass")
        {
            this.hexCoord = hexCoord;
            this.elevation = elevation;
            this.tileTypeId = tileTypeId;
            targetNestedMapId = String.Empty;
        }

        // HEAR ME OUT, TileBase contains all the data for World Tile, Stage Tile, and Floor tile, but only select stuff is shown based on map!
        // (makes the map editor logic and UI easier to make!
    }
}
