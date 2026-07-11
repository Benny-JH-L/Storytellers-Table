
using System.Collections.Generic;
using UnityEngine;
using StorytellersTable.Core.Data;
using StorytellersTable.Map;

namespace StorytellersTable.Renderer
{
    /// <summary>
    /// Handles map tile rendering, HexRenderers.
    /// </summary>
    public class MapTileRenderer : MonoBehaviour
    {
        [SerializeReference] private Dictionary<HexCoord, HexRenderer> tileVisuals = new Dictionary<HexCoord, HexRenderer>();

        /// <summary>
        /// Removes a HexRenderer, at the given hex coordinate <paramref name="data"/>.
        /// </summary>
        /// <param name="data"></param>
        public void RemoveVisual(TileData data)
        {
            HexCoord hexCoord = data.hexCoord;
            if (tileVisuals.ContainsKey(hexCoord)) 
            {
                Destroy(tileVisuals[hexCoord].gameObject);
                tileVisuals.Remove(hexCoord);
            }
        }

        /// <summary>
        /// Clears map HexRenderers.
        /// </summary>
        public void ClearVisuals()
        {
            // Destroy tile visuals
            foreach ((_, HexRenderer hexRenderer) in tileVisuals)
                Destroy(hexRenderer.gameObject);

            tileVisuals.Clear();
        }

        /// <summary>
        /// Generate a HexRenderer at the hex coordinate with material store in <paramref name="tileData"/> to the scene.
        /// </summary>
        /// <param name="tileData"></param>
        public void AddHexTileVisual(TileData tileData)
        {
            HexCoord hexCoord = tileData.hexCoord;
            HexRenderer hexRenderer = StorytellersTable.Campaign.Modes.MapEditMode.GenerateHexRenderer(hexCoord, tileData.GetMaterial());
            hexRenderer.transform.SetParent(this.transform, true);    // parent it

            // store the generated visual to the map
            tileVisuals.Add(hexCoord, hexRenderer);
        }

        public void AddHexTileVisual(List<TileData> tileDatas)
        {
            foreach (TileData data in tileDatas)
                AddHexTileVisual(data);
        }

        public void ReDrawMesh()
        {
            foreach ((_, HexRenderer hexRenderer) in tileVisuals)
                hexRenderer.DrawMesh();
        }
    }

}
