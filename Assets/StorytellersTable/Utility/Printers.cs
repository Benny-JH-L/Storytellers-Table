using System.Collections.Generic;

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
        }
    }
}
