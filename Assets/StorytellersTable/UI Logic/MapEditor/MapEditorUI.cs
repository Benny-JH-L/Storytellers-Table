
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

        [Header("UI Area")] // A section of the screen they take up
        public RectTransform selectionButtonGrpArea;

        [Header("Button groups")]
        public SelectionButtonGroup selectionButtonGrp;   // selection mode: single, radial, draw, area
                                                          // Modes: rmv, place, edit 
                                                          // change to label or tile

        [Header("Other")]
        private GameObject _confirmPlacementPrefab => Singleton.Instance.CancelConfirmBtn; // two buttons, 1 cancels, 1 confirms --> need to define an actual class for it
        private MapEditCancelConfirm confirmPlacementRuntime;

        public override void Setup()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            instance.transform.SetParent(Singleton.Instance.mainCanvas, false);

            selectionButtonGrp = GetComponentInChildren<SelectionButtonGroup>();
            // get rmv, place, edit
            // get change to label or tile
        }

        public override void Configure()
        {
            MapEditorContainer mapEditor = MapEditorContainer.instance;

            // Configure the selection buttons
            // Add listeners to switch selection modes
            selectionButtonGrp.singleSelBtn.button.onClick.AddListener(mapEditor.editModes.ToggleSingleSelect);
            selectionButtonGrp.areaSelBtn.button.onClick.AddListener(mapEditor.editModes.ToggleAreaSelect);
            selectionButtonGrp.radialSelBtn.button.onClick.AddListener(mapEditor.editModes.ToggleRadialSelect);
            selectionButtonGrp.drawSelBtn.button.onClick.AddListener(mapEditor.editModes.ToggleDrawSelect);
        }

        /// <summary>
        /// Loads the "Cancel" or "Confirm" UI, and sets the listeners when the "Cancel" or "Confirm" buttons are clicked.
        /// </summary>
        /// 
        /// <remarks>
        /// <paramref name="call1"/> is used for the "Cancel" Button and <paramref name="call2"/> is used for the "Confirm" Button. If the UI is already loaded, the listeners will cleared and updated.
        /// </remarks>
        /// <param name="call1"></param>
        /// <param name="call2"></param>
        public void LoadConfirmCancelUI(UnityEngine.Events.UnityAction call1, UnityEngine.Events.UnityAction call2)
        {
            if (confirmPlacementRuntime != null)
            {
                confirmPlacementRuntime.cancelBtn.onClick.RemoveAllListeners();
                confirmPlacementRuntime.confirmBtn.onClick.RemoveAllListeners();

                confirmPlacementRuntime.cancelBtn.onClick.AddListener(call1);
                confirmPlacementRuntime.confirmBtn.onClick.AddListener(call2);
                return;
            }

            confirmPlacementRuntime = Instantiate(_confirmPlacementPrefab, this.transform).GetComponent<MapEditCancelConfirm>();
            confirmPlacementRuntime.cancelBtn.onClick.AddListener(call1);
            confirmPlacementRuntime.confirmBtn.onClick.AddListener(call2);
        }

        /// <summary>
        /// Destroys the "Cancel" or "Confirm" UI.
        /// </summary>
        public void DestroyConfirmCancelUI()
        {
            if (confirmPlacementRuntime == null)
                return;

            Destroy(confirmPlacementRuntime.gameObject);
            confirmPlacementRuntime = null;
        }
    }
}
