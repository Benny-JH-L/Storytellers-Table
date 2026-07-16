
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
        /// Generate a HexRenderer at the hex coordinate with material store in <paramref name="tileData"/> to the scene.
        /// May also include a shader.
        /// </summary>
        /// <param name="tileData"></param>
        /// <param name="shader"></param>
        public void AddHexTileVisual(HexCoord hexCoord, Material material, Shader shader = null)
        {
            if (tileVisuals.ContainsKey(hexCoord))
                return;

            HexRenderer hexRenderer = StorytellersTable.Campaign.Modes.MapEditMode.GenerateHexRenderer(hexCoord, material);
            hexRenderer.transform.SetParent(this.transform, true);    // parent it

            if (shader != null)
                hexRenderer.SetMaterialShader(shader);

            // store the generated visual to the map
            tileVisuals.Add(hexCoord, hexRenderer);
        }

        public void AddHexTileVisual(List<HexCoord> datas, Material material, Shader shader = null)
        {
            foreach (HexCoord data in datas)
                AddHexTileVisual(data, material, shader);
        }

        /// <summary>
        /// Removes a HexRenderer, at the given hex coordinate <paramref name="data"/>.
        /// </summary>
        /// <param name="data"></param>
        public void RemoveVisual(HexCoord hexCoord)
        {
            if (tileVisuals.ContainsKey(hexCoord))
            {
                Destroy(tileVisuals[hexCoord].gameObject);
                tileVisuals.Remove(hexCoord);
            }
        }

        public void RemoveVisual(List<HexCoord> datas)
        {
            foreach (HexCoord data in datas)
                RemoveVisual(data);
        }


        #region tile highlight & selection & ghost visual

        /// <summary>
        /// Highlights tiles at HexCoords in <paramref name="datas"/>.
        /// </summary>
        /// 
        /// <remarks>Highlights tile if <paramref name="enable"/> is True. Disable's highlight otherwise.</remarks>
        /// <param name="datas"></param>
        /// <param name="enable"></param>
        public void EnableHighlight(List<HexCoord> datas, bool enable)
        {
            foreach (HexCoord hexCoord in datas)
                EnableHighlight(hexCoord, enable);
        }

        /// <summary>
        /// Highlights tile at <paramref name="hexCoord"/>.
        /// </summary>
        /// 
        /// <remarks>Highlights tile if <paramref name="enable"/> is True. Disable's highlight otherwise.</remarks>
        /// <param name="datas"></param>
        /// <param name="enable"></param>
        public void EnableHighlight(HexCoord hexCoord, bool enable)
        {
            if (!tileVisuals.ContainsKey(hexCoord))
                return;

            tileVisuals[hexCoord].EnableHighlight(enable);
        }

        public void EnableSelectedVisual(List<HexCoord> datas, bool enable)
        {
            foreach (HexCoord hexCoord in datas)
                EnableSelectedVisual(hexCoord, enable);
        }

        public void EnableSelectedVisual(HexCoord hexCoord, bool enable)
        {
            if (!tileVisuals.ContainsKey(hexCoord))
                return;

            tileVisuals[hexCoord].SetSelectedVisual(enable);
        }

        public void EnableGhostVisual(List<HexCoord> datas, bool enable)
        {
            foreach (HexCoord hexCoord in datas)
                EnableGhostVisual(hexCoord, enable);
        }

        public void EnableGhostVisual(HexCoord hexCoord, bool enable)
        {
            if (!tileVisuals.ContainsKey(hexCoord))
                return;

            tileVisuals[hexCoord].SetGhostVisual(enable);
        }

        public void DisableAllHighlights()
        {
            foreach ((_, HexRenderer HexRenderer) in tileVisuals)
                HexRenderer.EnableHighlight(false);
        }

        /// <summary>
        /// Disables all highlights at HexCoords except those specified in the <paramref name="hexCoordsSet"/>.
        /// </summary>
        /// <remarks>
        /// If a coordinate does not exist in the MapTileRenderer but does in <paramref name="hexCoordsSet"/> nothing will happen.
        /// </remarks>
        /// <param name="hexCoordsSet"></param>
        public void DisableAllHighlightsExcept(HashSet<HexCoord> hexCoordsSet)
        {
            foreach (HexCoord hexCoord in tileVisuals.Keys)
            {
                if (hexCoordsSet.Contains(hexCoord))
                    continue;

                // Disable highlight
                EnableHighlight(hexCoord, false);
            }
        }

        public void DisableAllSelectedVisuals()
        {
            foreach ((_, HexRenderer HexRenderer) in tileVisuals)
                HexRenderer.SetSelectedVisual(false);
        }

        public void DisableAllGhostVisuals()
        {
            foreach ((_, HexRenderer HexRenderer) in tileVisuals)
                HexRenderer.SetGhostVisual(false);
        }

        #endregion

        ///// <summary>
        ///// Add visual data from another MapTileRenderer instance to this instance, cannot be null.
        ///// </summary>
        ///// 
        ///// <remarks>
        ///// If <paramref name="clearOtherRenderer"/> is true, <paramref name="mapTileRenderer"/> will be cleared.
        ///// </remarks>
        ///// <param name="mapTileRenderer"></param>
        ///// <param name="clearOtherRenderer"></param>
        //public void AddFromMapTileRenderer(MapTileRenderer mapTileRenderer, bool clearOtherRenderer = false)
        //{
        //    if (mapTileRenderer == this || mapTileRenderer == null)
        //        return;

        //    foreach (var pair in mapTileRenderer.GetVisualData())
        //        tileVisuals[pair.Key] = pair.Value;

        //    if (clearOtherRenderer)
        //        mapTileRenderer.ClearVisuals(); // this functin does not work bc i destroy the visual anyway...
        //}

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

        public void ReDrawMesh()
        {
            foreach ((_, HexRenderer hexRenderer) in tileVisuals)
                hexRenderer.DrawMesh();
        }

        ///// <summary>
        ///// Return's the renderer's data, readonly.
        ///// </summary>
        ///// <returns></returns>
        //public IReadOnlyCollection<Dictionary<HexCoord, HexRenderer>> GetVisualData()
        //{
        //    return (IReadOnlyCollection<Dictionary<HexCoord, HexRenderer>>)tileVisuals.AsReadOnlyCollection();
        //}

        /// <summary>
        /// Return's the renderer's data.
        /// </summary>
        /// <returns></returns>
        public Dictionary<HexCoord, HexRenderer> GetVisualData()
        {
            return tileVisuals;
        }

        /// <summary>
        /// Return's the number of Hexrenderers handled by this instance.
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            return tileVisuals.Count;
        }
    }

}
