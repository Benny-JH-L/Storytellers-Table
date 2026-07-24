
using StorytellersTable.Campaign.Modes;
using StorytellersTable.Utility.Log;
using UnityEngine;

namespace StorytellersTable.UiLogic
{
    /// <summary>
    /// Handles the UI for the Map editor.
    /// </summary>
    [DisallowMultipleComponent]
    public class MapEditorUIManager : CustomUIComponent
    {
        public static MapEditorUIManager instance;

        [Header("UI Area")] // A section of the screen they take up
        public RectTransform selectionButtonGrpArea;

        [Header("Button groups")]
        public SelectionButtonGroup selectionButtonGrp;   // selection mode: single, radial, draw, area
        public EditModeButtonGroup editModeButtonGroup;   // Modes: rmv, place, edit 
        public TileLabelToggleButton tileLabelToggleButton;   // change to label or tile

        [Header("Other")]
        private GameObject _confirmPlacementPrefab => Singleton.Instance.CancelConfirmBtn; // two buttons, 1 cancels, 1 confirms --> need to define an actual class for it
        private MapEditCancelConfirm confirmPlacementRuntime;
        // label to display "Map Editor - <selection mode> - <place/rmv/edit> - <tile/label>"

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
            editModeButtonGroup = GetComponentInChildren<EditModeButtonGroup>();
            tileLabelToggleButton = GetComponentInChildren<TileLabelToggleButton>();
        }

        public override void Configure()
        {
            MapEditorContainer mapEditor = MapEditorContainer.instance;

            // Configure the selection buttons
            // Add listeners to switch selection modes
            ModeContainer modeContainer = mapEditor.editModes;
            selectionButtonGrp.singleSelBtn.button.onClick.AddListener(modeContainer.ToggleSingleSelect);
            selectionButtonGrp.areaSelBtn.button.onClick.AddListener(modeContainer.ToggleAreaSelect);
            selectionButtonGrp.radialSelBtn.button.onClick.AddListener(modeContainer.ToggleRadialSelect);
            selectionButtonGrp.drawSelBtn.button.onClick.AddListener(modeContainer.ToggleDrawSelect);

            // Add listeners to switch editing modes
            editModeButtonGroup.removeBtn.button.onClick.AddListener(modeContainer.ToggleRemove);
            editModeButtonGroup.placeBtn.button.onClick.AddListener(modeContainer.TogglePlace);
            editModeButtonGroup.editBtn.button.onClick.AddListener(modeContainer.ToggleEdit);

            // Add listeners to switch tile label mode
            //tileLabelToggleButton.toggleButton.button.onClick.AddListener(modeContainer.ToggleTileMode);
            //tileLabelToggleButton.toggleButton.button.onClick.AddListener(tileLabelToggleButton.UpdateAppearance);
            tileLabelToggleButton.AddListener(modeContainer.ToggleTileMode);
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
