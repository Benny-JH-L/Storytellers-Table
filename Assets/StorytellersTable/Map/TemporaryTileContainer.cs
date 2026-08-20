using Assets.StorytellersTable.Core.Map;
using StorytellersTable.Map;
using StorytellersTable.Renderer;
using UnityEngine;

namespace StorytellersTable.Campaign.Modes
{
    public class TemporaryTileContainer : MonoBehaviour
    {
        public MapTileRenderer TileRenderer { get; private set; }
        public MapTileRepresentation TmpTiles { get; private set; }


        private void Awake()
        {
            TileRenderer = new GameObject("Temporary_Tile_MapTileRenderer", typeof(MapTileRenderer)).GetComponent<MapTileRenderer>();
            TileRenderer.transform.SetParent(this.transform);

            TmpTiles = new();
        }

        public void Add(UpdateMapInfoPackage package)
        {
            TileRenderer.AddHexTileVisual(package);
            TileRenderer.EnableGhostVisual(package, true);

            TmpTiles.AddTiles(package);
        }

        public void Clear()
        {
            TileRenderer.ClearVisuals();
            TmpTiles.Clear();
        }
    }
}
