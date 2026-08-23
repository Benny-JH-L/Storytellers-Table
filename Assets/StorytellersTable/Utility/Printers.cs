using Assets.StorytellersTable.Core.Map;
using StorytellersTable.Map;
using StorytellersTable.Renderer;
using System.Collections.Generic;
using UnityEngine;

namespace StorytellersTable.Utility.Printer
{
    public static class Printer
    {
        public static void Print<T>(List<T> list, string prepend = "")
        {
            string s  = prepend;
            foreach (T item in list)
            {
                s += item.ToString() + ", ";
            }
            Debug.Log(s);
        }

        public static void Print(Dictionary<int, HashSet<HexCoord>> dict)
        {
            string s = "{\n";
            foreach ((int i, var hashSet) in dict)
            {
                s += $"[{i}: ";
                foreach (var hexCoord in hashSet) 
                    s += hexCoord.ToString() + ", ";
                s += "], \n";
            }
            s += "}";
            Debug.Log(s);
        }

        public static void Print(UpdateMapInfoPackage updateMapInfoPackage)
        {
            Printer.Print(Conversion.ToMapTileRendererPackage(updateMapInfoPackage));
        }

        public static void Print(MapTileRendererPackage mapTileRendererPackage)
        {
            string s = "{\n";
            var dict = mapTileRendererPackage.info;
            foreach ((Layer i, var hashSet) in dict)
            {
                s += $"[{i}: ";
                foreach (var hexCoord in hashSet)
                    s += hexCoord.ToString() + ", ";
                s += "], \n";
            }
            s += "}";
            Debug.Log(s);
        }
    }
}
