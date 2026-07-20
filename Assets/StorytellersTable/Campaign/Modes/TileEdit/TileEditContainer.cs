
using StorytellersTable.Core.Data;
using StorytellersTable.Map;
using StorytellersTable.Renderer;
using StorytellersTable.Utility.Log;
using StorytellersTable.Utility.Printer;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace StorytellersTable.Campaign.Modes
{
    /// <summary>
    /// Tile editing of the active map.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-500)]
    public class TileEditContainer : MonoBehaviour
    {
        public static TileEditContainer instance;

        [Header("Ui")]
        [SerializeField] private Transform uiParentTransform;
        // UI Prefab
        [SerializeField] private GameObject tileInfoUIPrefab;
        [SerializeField] private GameObject runtimeUI;

        [Header("Edited by UI - Changing values")]
        // values edit by the UI
        [SerializeField] private string newTileTypeId = string.Empty;
        [SerializeField] private int newHeight = 1;

        // tile coords to edit
        private List<HexCoord> hexCoordsToEdit;

        private MapManager mapManager => MapManager.Instance;

        // Is this container active
        public bool IsActive { get; private set; }

        private void Awake()
        {
            if (instance != this && instance != null)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private void OnEnable()
        {
            uiParentTransform = Singleton.Instance.mainCanvas.transform;

            hexCoordsToEdit = new List<HexCoord>();
            runtimeUI = null;
            IsActive = false;

            // Set default material
            if (newTileTypeId == string.Empty)
                newTileTypeId = MaterialLoader.instance.defaultMaterialName;
        }

        public void SetValues(HashSet<HexCoord> hexCoords)
        {
            hexCoordsToEdit.Clear();
            hexCoordsToEdit.AddRange(hexCoords);
        }

        public void ResetValues()
        {
            hexCoordsToEdit.Clear();
        }

        public void ConfirmHeightEdit()
        {
            ConfirmEdits(true, false);
        }

        public void ConfirmMaterialEdit()
        {
            ConfirmEdits(false, true);
        }


        public void ConfirmAllEdits()
        {
            ConfirmEdits(true, true);
        }

        private void ConfirmEdits(bool updateHeight, bool updateMaterialId)
        {
            if (hexCoordsToEdit.Count == 0)
                return;

            //Printer.Print(hexCoordsToEdit);
            //DebugOut.Log(this, $"update height = {updateHeight} | updateMaterialId = {updateMaterialId}");

            HashSet<HexCoord> activeMapTiles = mapManager.ActiveMapData.tileDatas.Keys.ToHashSet();
            HashSet<HexCoord> tilesToEdit = hexCoordsToEdit.ToHashSet();
            Dictionary<HexCoord, HexRenderer> mapTileRenderer = mapManager.mapTileRenderer.GetVisualData();

            foreach (HexCoord hexCoord in activeMapTiles)
            {
                if (!tilesToEdit.Contains(hexCoord))
                    continue;

                HexRenderer hexRenderer = mapTileRenderer[hexCoord];
                TileData tileData = mapManager.ActiveMapData.tileDatas[hexCoord];

                // Edit tile data
                if (updateHeight)
                {
                    tileData.height = newHeight;
                    hexRenderer.height = newHeight;
                }
                if (updateMaterialId)
                    tileData.tileTypeId = newTileTypeId;
                // Set the new data
                MapManager.Instance.SetNewTileData(tileData);

                // Update HexRenderer                
                hexRenderer.SetSharedMaterial(tileData.GetMaterial());
                hexRenderer.DrawMesh();
            }
        }

        public void Activate()
        {
            DebugOut.Log(this, "Enabled TileEditMode");

            if (runtimeUI == null)
            {
                // Enable UI
                runtimeUI = GameObject.Instantiate(tileInfoUIPrefab, uiParentTransform);
            }

            IsActive = true;
        }

        public void Disable()
        {
            DebugOut.Log(this, "Disabled TileEditMode");

            if (runtimeUI != null)
                Destroy(runtimeUI.gameObject);

            runtimeUI = null;
            IsActive = false;
        }

        public void SetMaterialId(string materialId)
        {
            if (materialId == string.Empty)
                return;

            newTileTypeId = materialId;
        }

        public void SetHeight(int height)
        {
            newHeight = height;
        }
    }

}
