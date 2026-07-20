
using StorytellersTable.Campaign.Modes;
using StorytellersTable.Utility.Log;
using UnityEngine;

namespace StorytellersTable.UiLogic
{
    /// <summary>
    /// Handles the UI for the Map editor.
    /// </summary>
    [DisallowMultipleComponent]
    public class MapEditorUI : CustomUIComponent
    {
        public static MapEditorUI instance;

        [Header("Selection Types Prefab")]
        [SerializeField] private SelectionButtonGroup selectionButtonGrpPrefab;

        //[Header("Rmv, Place, Edit tiles")]
        //// ...

        //[Header("Swap to label")]
        //// ...

        [Header("UI Area")] // A section of the screen they take up
        public RectTransform selectionButtonGrpArea;

        [Header("Runtime UI")]
        [SerializeField] private SelectionButtonGroup selectionButtonGrpRuntime;

        public override void Setup()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            instance.transform.SetParent(Singleton.Instance.mainCanvas, false);

            selectionButtonGrpPrefab = Singleton.Instance.selectionButtonGrpPrefab.GetComponent<SelectionButtonGroup>();
        }

        public override void Configure()
        {
            MapEditorContainer mapEditor = MapEditorContainer.instance;

            // Configure the selection buttons
            selectionButtonGrpRuntime = Instantiate(selectionButtonGrpPrefab, selectionButtonGrpArea.transform).GetComponent<SelectionButtonGroup>();
            // Add listeners to switch selection modes
            selectionButtonGrpRuntime.singleSelBtn.button.onClick.AddListener(mapEditor.editModes.ToggleSingleSelect);
            selectionButtonGrpRuntime.areaSelBtn.button.onClick.AddListener(mapEditor.editModes.ToggleAreaSelect);
            selectionButtonGrpRuntime.radialSelBtn.button.onClick.AddListener(mapEditor.editModes.ToggleRadialSelect);
            selectionButtonGrpRuntime.drawSelBtn.button.onClick.AddListener(mapEditor.editModes.ToggleDrawSelect);
        }
    }
}
