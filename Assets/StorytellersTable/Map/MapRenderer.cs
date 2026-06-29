using UnityEngine;
using StorytellersTable.Core.Data;

namespace StorytellersTable.Map
{
    /// <summary>
    /// Contains the map visuals: tiles, map text, etc.
    /// </summary>
    public class MapRenderer : MonoBehaviour
    {
        public static MapRenderer Instance { get; private set; }
        public MapData activeMap => MapDataManager.Instance.ActiveMapData;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }


    }
}