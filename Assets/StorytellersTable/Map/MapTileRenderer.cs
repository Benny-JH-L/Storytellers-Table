using Assets.StorytellersTable.Core.Map;
using StorytellersTable.Map;
using System.Collections.Generic;
using UnityEngine;

namespace StorytellersTable.Renderer
{
    public struct MapTileRendererPackage
    {
        /// <summary>
        /// Key: layer,
        /// value: set of hex coordinates.
        /// </summary>
        public Dictionary<Layer, HashSet<HexCoord>> info;
    }

    /// <summary>
    /// Handles map tile rendering, HexRenderers.
    /// </summary>
    public class MapTileRenderer : MonoBehaviour
    {
        /// <summary>
        /// Key: layer,
        /// Value: Tile visual on that layer with hex coord.
        /// </summary>
        private readonly Dictionary<Layer, Dictionary<HexCoord, HexRenderer>> tileVisuals = new();

        /// <summary>
        /// Generate a HexRenderer at the hex coordinate and at <paramref name="layer"/> with material, <paramref name="materialName"/>, to the scene.
        /// May also include a shader.
        /// </summary>
        /// <param name="hexCoord"></param>
        /// <param name="materialName"></param>
        /// <param name="layer"></param>
        /// <param name="shader"></param>
        public void AddHexTileVisual(HexCoord hexCoord, string materialName, Layer layer, Shader shader = null)
        {
            if (!tileVisuals.ContainsKey(layer))
                tileVisuals[layer] = new();

            Material material = MaterialLoader.instance.GetMaterial(materialName);

            // hex renderer exists already, update the material
            if (tileVisuals[layer].TryGetValue(hexCoord, out HexRenderer existingRenderer))
            {
                existingRenderer.SetSharedMaterial(material);
                return;
            }

            HexRenderer hexRenderer = StorytellersTable.Campaign.Modes.MapEditorContainer.GenerateHexRenderer(hexCoord, material, layer);
            hexRenderer.transform.SetParent(this.transform, true);    // parent it

            if (shader != null)
                hexRenderer.SetMaterialShader(shader);

            // store the generated visual to the map
            tileVisuals[layer].Add(hexCoord, hexRenderer);
        }

        public void AddHexTileVisual(UpdateMapInfoPackage pacakge, Shader shader = null)
        {
            foreach (TileData data in pacakge.info)
                AddHexTileVisual(data.hexCoord, data.materialId, data.mapLayer, shader);
        }

        public void AddHexTileVisual(TileData tileData, Shader shader = null)
        {
            AddHexTileVisual(tileData.hexCoord, tileData.materialId, tileData.mapLayer, shader);
        }

        /// <summary>
        /// Takes the visual info from <paramref name="mapTileRenderer"/> and puts it into this instance.
        /// If this MapTileRenderer contains a entry that also exists in <paramref name="mapTileRenderer"/> and 
        /// <paramref name="replaceExistingHexRenderer"/> is `true`, it will destroy its own entry and 
        /// replace it with the one in <paramref name="mapTileRenderer"/>.
        /// </summary>
        /// 
        /// <remarks>
        /// Note: the HexRenderer stored in <paramref name="mapTileRenderer"/> will be "stolen" and put into this instance.
        /// </remarks>
        /// <param name="mapTileRenderer"></param>
        /// <param name="replaceExistingHexRenderer"></param>
        public void AddFromMapTileRenderer(MapTileRenderer mapTileRenderer, bool replaceExistingHexRenderer = false)
        {
            foreach ((var layer, var dict) in mapTileRenderer.GetVisualData())
            {
                // check if this map tile renderer has this layer
                if (!tileVisuals.TryGetValue(layer, out var _))
                    tileVisuals[layer] = new();

                foreach ((HexCoord hexCoord, HexRenderer hexRendererToSteal) in dict)
                {
                    // A hex renderer already exists with the given entry: [layer][hexCoord], skip it (if `replaceExistingHexRenderer` is also false)
                    if (tileVisuals[layer].TryGetValue(hexCoord, out HexRenderer _))
                    {
                        if (!replaceExistingHexRenderer)
                            continue;

                        // Destroy existing HexRenderer and remove entry
                        Destroy(tileVisuals[layer][hexCoord].gameObject);
                        tileVisuals[layer].Remove(hexCoord);
                    }

                    // add to tile visuals and set new parent for the HexRenderer
                    tileVisuals[layer].Add(hexCoord, hexRendererToSteal);
                    hexRendererToSteal.transform.SetParent(this.transform, false);
                }
            }
        }

        /// <summary>
        /// Removes a HexRenderer, at the given hex coordinate <paramref name="data"/>.
        /// </summary>
        /// <param name="data"></param>
        public void RemoveVisual(HexCoord hexCoord, Layer layer)
        {
            if (EntryExists(hexCoord, layer))
            {
                Destroy(tileVisuals[layer][hexCoord].gameObject);
                tileVisuals[layer].Remove(hexCoord);
            }
        }

        public void RemoveVisual(MapTileRendererPackage package)
        {
            foreach ((Layer layer, HashSet<HexCoord> hexSet) in package.info)
            {
                foreach (HexCoord hexCoord in hexSet)
                    RemoveVisual(hexCoord, layer);
            }
        }

        #region tile highlight & selection & ghost visual

        /// <summary>
        /// Highlights tiles at HexCoords in <paramref name="datas"/>.
        /// </summary>
        /// 
        /// <remarks>Highlights tile if <paramref name="enable"/> is True. Disable's highlight otherwise.</remarks>
        /// <param name="datas"></param>
        /// <param name="enable"></param>
        public void EnableHighlight(UpdateMapInfoPackage package, bool enable)
        {
            foreach (TileData data in package.info)
                EnableHighlight(data.hexCoord, data.mapLayer, enable);
        }

        /// <summary>
        /// Highlights tile at <paramref name="hexCoord"/>.
        /// </summary>
        /// 
        /// <remarks>Highlights tile if <paramref name="enable"/> is True. Disable's highlight otherwise.</remarks>
        /// <param name="datas"></param>
        /// <param name="enable"></param>
        public void EnableHighlight(HexCoord hexCoord, Layer layer, bool enable)
        {
            if (!EntryExists(hexCoord, layer))
                return;

            tileVisuals[layer][hexCoord].EnableHighlight(enable);
        }

        public void EnableSelectedVisual(UpdateMapInfoPackage package, bool enable)
        {
            foreach (TileData data in package.info)
                EnableSelectedVisual(data.hexCoord, data.mapLayer, enable);
        }

        public void EnableSelectedVisual(HexCoord hexCoord, Layer layer, bool enable)
        {
            if (!EntryExists(hexCoord, layer))
                return;

            tileVisuals[layer][hexCoord].SetSelectedVisual(enable);
        }

        public void EnableGhostVisual(UpdateMapInfoPackage package, bool enable)
        {
            foreach (TileData data in package.info)
                EnableGhostVisual(data.hexCoord, data.mapLayer, enable);
        }

        public void EnableGhostVisual(HexCoord hexCoord, Layer layer, bool enable)
        {
            if (!EntryExists(hexCoord, layer))
                return;

            tileVisuals[layer][hexCoord].SetGhostVisual(enable);
        }

        public void DisableAllHighlights()
        {
            foreach ((_, var dict) in  tileVisuals)
            {
                foreach ((_, HexRenderer HexRenderer) in dict)
                    HexRenderer.EnableHighlight(false);
            }
        }

        /// <summary>
        /// Disables all highlights at HexCoords except those specified in the <paramref name="hexCoordsSet"/>.
        /// </summary>
        /// <remarks>
        /// If a coordinate does not exist in the MapTileRenderer but does in <paramref name="hexCoordsSet"/> nothing will happen.
        /// </remarks>
        /// <param name="hexCoordsSet"></param>
        public void DisableAllHighlightsExcept(MapTileRendererPackage package)
        {
            foreach ((var layer, var dict) in tileVisuals)
            {
                // check if this layer has tiles to disable highlight
                if (!package.info.ContainsKey(layer))
                    continue;

                // go disable highlights that both exist visually and in the package.
                foreach ((HexCoord hexCoord, _) in dict)
                {
                    if (package.info[layer].Contains(hexCoord))
                        EnableHighlight(hexCoord, layer, false);
                }
            }
        }

        public void DisableAllSelectedVisuals()
        {
            foreach ((_, var dict) in tileVisuals)
            {
                foreach ((_, HexRenderer HexRenderer) in dict)
                    HexRenderer.SetSelectedVisual(false);
            }
        }

        public void DisableAllGhostVisuals()
        {
            foreach ((_, var dict) in tileVisuals)
            {
                foreach ((_, HexRenderer HexRenderer) in dict)
                    HexRenderer.SetGhostVisual(false);
            }
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
            foreach (var dict in tileVisuals.Values)
            {
                foreach (HexRenderer hexRenderer in dict.Values)
                    Destroy(hexRenderer.gameObject);
                dict.Clear();
            }

            tileVisuals.Clear();
        }

        public void ReDrawMesh()
        {
            foreach (var dict in tileVisuals.Values)
            {
                foreach (HexRenderer hexRenderer in dict.Values)
                    hexRenderer.DrawMesh();
            }
        }

        private bool EntryExists(HexCoord hexCoord, Layer layer)
        {
            if (!tileVisuals.ContainsKey(layer))
                return false;
            if (!tileVisuals[layer].ContainsKey(hexCoord))
                return false;
            return true;
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
        public Dictionary<Layer, Dictionary<HexCoord, HexRenderer>> GetVisualData()
        {
            return tileVisuals;
        }

        /// <summary>
        /// Return's the number of Hexrenderers handled by this instance.
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            int count = 0;
            foreach (var dict in tileVisuals.Values)
                count += dict.Count;
            return count;
        }
    }

}
