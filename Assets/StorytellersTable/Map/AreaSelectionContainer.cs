
using Assets.StorytellersTable.Core.Map;
using StorytellersTable.Campaign.Modes;
using StorytellersTable.Map;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace StorytellersTable.Campaign.Modes
{
    public struct AreaSelectPayload
    {
        public HexCoord Start { get; private set; }
        public HexCoord End { get; private set; }
        public Layer LayerStart { get; private set; }
        public Layer LayerEnd { get; private set; }
        public bool StartDefined { get; private set; }
        public bool EndDefined { get; private set; }

        public AreaSelectPayload(HexCoord start, HexCoord end, Layer layerStart, Layer layerEnd)
        {
            Start = start;
            End = end;
            LayerStart = layerStart;
            LayerEnd = layerEnd;
            StartDefined = true;
            EndDefined = true;
        }

        public void SetStart(HexCoord start, Layer layerStart)
        {
            StartDefined = true;
            Start = start;
            LayerStart = layerStart;
        }

        public void SetEnd(HexCoord end, Layer layerEnd)
        {
            EndDefined = true;
            End = end;
            LayerEnd = layerEnd;
        }

        public bool IsValid()
        {
            return StartDefined && EndDefined && (LayerEnd is not null && LayerStart is not null);
        }
    }

    public class AreaSelectionContainer
    {
        public TileData Start {  get; private set; }
        public TileData End { get; private set; }
        //MapTileRepresentation mapTileRepresentation => MapManager.Instance.ActiveMapData.mapTileData;
        public MapTileRepresentation MapTileRep { get; set; }

        public AreaSelectionContainer(MapTileRepresentation mapTileRepresentation)
        {
            //SelectionContainer = selectionContainer;
            MapTileRep = mapTileRepresentation;
            //areaSelectPayload = new();
        }

        public void SetStart(TileData start)
        {
            Start = start;
        }

        public void SetEnd(TileData end, bool layerFocusOn, out HashSet<TileData> areaResult)
        {
            End = end;
            UpdateAreaSelection(layerFocusOn, out areaResult);
        }

        /// <summary>
        /// Using the `start` and `end` hex coords and at their respective layers, get all the TileData's within the area.
        /// </summary>
        /// <param name="areaResult"></param>
        private void UpdateAreaSelection(bool layerFocusOn, out HashSet<TileData> areaResult)
        {
            areaResult = new();

            HashSet<HexCoord> coordResult = new();
            HexMath.GetAreaAxial(Start.hexCoord, End.hexCoord, coordResult);    // get hex coords in the area

            if (!layerFocusOn)
            {
                // get surface tile located at each hex coord
                foreach (HexCoord coord in coordResult)
                {
                    MapTileRep.GetTileDataStack(coord, out List<TileData> stack);
                    // add the surface tile
                    if (stack.Count > 0)
                        areaResult.Add(stack[0]);
                }
            }
            else
            {
                int start = Mathf.Min(Start.mapLayer.Val, End.mapLayer.Val);
                int end = Mathf.Max(Start.mapLayer.Val, End.mapLayer.Val);

                // go through each layer between the `start` and `end` (inclusive)
                for (int layer = start; layer <= end; layer++)
                {
                    // foreach hex coord in the area in that layer, grab only those that exist in the map data
                    foreach (HexCoord coord in coordResult)
                    {
                        if (MapTileRep.TryGet(new Layer(layer), coord, out TileData result))
                            areaResult.Add(result);
                    }
                }
            }
        }

        public void Reset()
        {
            Start = null;
            End = null;
        }
    }

}
