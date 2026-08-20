using Assets.StorytellersTable.Core.Map;
using StorytellersTable.Map;
using StorytellersTable.Renderer;
using System.Collections.Generic;

namespace StorytellersTable.Utility
{
    public static class Conversion
    {
        public static MapTileRendererPackage ToMapTileRendererPackage(UpdateMapInfoPackage package)
        {
            Dictionary<Layer, HashSet<HexCoord>> newInfo = new();

            foreach(TileData data in package.info)
            {
                Layer layer = data.mapLayer;
                
                if (!newInfo.ContainsKey(layer))
                    newInfo[layer] = new();

                newInfo[layer].Add(data.hexCoord);
            }

            MapTileRendererPackage newPackage = new() { info = newInfo };
            return newPackage;
        }

        public static UpdateMapInfoPackage ToUpdateMapInfoPackage(MapTileRepresentation info)
        {
            HashSet<TileData> tmpSet = new();

            foreach ((_, var dict) in info.GetTileRepresentation())
            {
                foreach (TileData tileData in dict.Values)
                    tmpSet.Add(tileData);
            }

            return new UpdateMapInfoPackage() { info = tmpSet};
        }
    }
}
