
using NUnit.Framework.Constraints;
using StorytellersTable.Core.Data;
using StorytellersTable.Map;
using StorytellersTable.Renderer;
using StorytellersTable.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace StorytellersTable.Campaign.Modes
{
    /// <summary>
    /// Container to keep track of selected (confirmed) and selecting (unconfirmed) tiles. As well as handle relavent visual states, for said tiles.
    /// </summary>
    public class SelectionContainer : MonoBehaviour
    {
        public MapTileRepresentation UnconfirmedTiles { get; private set; }
        public MapTileRepresentation ConfirmedTiles { get; private set; }
        private MapTileRenderer currentMapRenderer;

        private void Awake()
        {
            UnconfirmedTiles = new MapTileRepresentation();
            ConfirmedTiles = new MapTileRepresentation();

            Init();
        }

        public void Init()
        {
            if (currentMapRenderer != null)
                ResetVisualStates();

            // Set the active map's tile renderer
            currentMapRenderer = MapManager.Instance.mapTileRenderer;
        }

        public void AddUnconfirmed(UpdateMapInfoPackage package)
        {
            UnconfirmedTiles.AddTiles(package);
            SetUnconfirmedVisualState(package, true);
        }

        public void RemoveFromUnconfirmed(UpdateMapInfoPackage package)
        {
            UnconfirmedTiles.TryRemoveMultiple(package);
            SetUnconfirmedVisualState(package, false);
        }

        /// <summary>
        /// Utilizes the unconfirmed state of items and switches them to a confirmed state.
        /// </summary>
        public void UpdateConfirmed()
        {
            ConfirmedTiles.AddFromMapTileRepresentation(UnconfirmedTiles);
            SetConfirmedVisualState(Conversion.ToUpdateMapInfoPackage(UnconfirmedTiles), true);
        }

        public void RemoveFromConfirmed(UpdateMapInfoPackage package)
        {
            ConfirmedTiles.TryRemoveMultiple(package);
            SetConfirmedVisualState(package, false);
        }

        /// <summary>
        /// Set visual state of tiles described in <paramref name="package"/> based on <paramref name="val"/>, `true` to enable, `false` to disable.
        /// </summary>
        /// <param name="package"></param>
        /// <param name="val"></param>
        private void SetUnconfirmedVisualState(UpdateMapInfoPackage package, bool val)
        {
            currentMapRenderer.EnableHighlight(package, val);
        }

        /// <summary>
        /// Set visual state of tiles described in <paramref name="package"/> based on <paramref name="val"/>, `true` to enable, `false` to disable.
        /// </summary>
        /// <param name="package"></param>
        /// <param name="val"></param>
        private void SetConfirmedVisualState(UpdateMapInfoPackage package, bool val)
        {
            currentMapRenderer.EnableSelectedVisual(package, val);
        }

        /// <summary>
        /// Turns off all visual state modifications such as highlight, ghost, and active selection on the active map.
        /// </summary>
        public void ResetVisualStates()
        {
            currentMapRenderer.DisableAllHighlights();
            currentMapRenderer.DisableAllGhostVisuals();
            currentMapRenderer.DisableAllSelectedVisuals();
        }

        public void ClearUnconfirmed()
        {
            SetUnconfirmedVisualState(Conversion.ToUpdateMapInfoPackage(UnconfirmedTiles), false);
            UnconfirmedTiles.Clear();
        }

        public void ClearUnconfirmedExcept(UpdateMapInfoPackage package)
        {
            HashSet<TileData> toRmv = new();
            foreach (var dict in UnconfirmedTiles.GetTileRepresentation().Values)
            {
                foreach (TileData data in dict.Values)
                {
                    if (package.info.Contains(data))
                        continue;
                    toRmv.Add(data);
                }
            }

            UpdateMapInfoPackage newPack = new() { info = toRmv };
            SetUnconfirmedVisualState(newPack, false);      // set visual state
            UnconfirmedTiles.TryRemoveMultiple(newPack);    // remove entry
        }

        public void ClearConfirmed()
        {
            SetConfirmedVisualState(Conversion.ToUpdateMapInfoPackage(ConfirmedTiles), false);
            ConfirmedTiles.Clear();
        }
    }
}
