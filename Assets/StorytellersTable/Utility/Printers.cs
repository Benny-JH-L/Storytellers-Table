using System.Collections.Generic;
using UnityEngine;

namespace StorytellersTable.Utility.Printer
{
    public static class Printer
    {
        public static void Print<T>(List<T> list)
        {
            string s  = string.Empty;
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
    }
}
