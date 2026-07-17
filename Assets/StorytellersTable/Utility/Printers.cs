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
    }
}
