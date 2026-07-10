
using System;
using UnityEngine.InputSystem;
using StorytellersTable.Utility.Log;

namespace StorytellersTable.Campaign.Modes
{
    /// <summary>
    /// Available selection modes.
    /// </summary>
    public enum SelectModeTypes
    {
        singleSelect,
        radialSelect,
        areaSelect,
        drawSelect,
    }

    /// <summary>
    /// Available modes for map editing.
    /// </summary>
    public enum EditModeTypes
    {
        tilePlace,
        tileRemove,
        tileEdit,
        labelPlace,
        labelRemove,
        labelEdit,
    }

    /// <summary>
    /// Encapsulates placement, edit, and removal mode selection.
    /// </summary>
    [Serializable]
    public class ModeContainer
    {
        public SelectModeTypes SelectionMode { get; private set; }
        public EditModeTypes EditMode { get; private set; }

        // True: tile mode. False: label mode
        private bool tileModeOn;

        public ModeContainer()
        {
            // enabled by default
            SelectionMode = SelectModeTypes.singleSelect;
            EditMode = EditModeTypes.tileEdit;
            tileModeOn = true;
        }

        #region edit modes

        public void ToggleTileMode(InputAction.CallbackContext context)
        {
            ToggleTileMode();
        }

        /// <summary>
        /// Toggles on or off tile/label mode, only one mode is enabled at a time (Tile or Label).
        /// </summary>
        public void ToggleTileMode()
        {
            // toggle bool
            tileModeOn = !tileModeOn;
            DebugOut.Log(this, $"Tile Mode: {tileModeOn} (False means label mode enabled)");

            // Update to the correct mode type
            switch (EditMode)
            {
                case EditModeTypes.tilePlace:
                    TogglePlace();
                    break;
                case EditModeTypes.tileRemove:
                    ToggleRemove();
                    break;
                case EditModeTypes.tileEdit:
                    ToggleEdit();
                    break;
            }
        }

        public void ToggleEdit(InputAction.CallbackContext context)
        {
            ToggleEdit();
        }

        /// <summary>
        /// Enables edit mode for tile/label. NOTE if is already enabled, it will stay enabled.
        /// </summary>
        public void ToggleEdit()
        {
            if (tileModeOn)
            {
                EditMode = EditModeTypes.tileEdit;
            }
            else
            {
                EditMode = EditModeTypes.labelEdit;
            }

            DebugOut.Log(this, $"Edit enabled.");
        }

        public void TogglePlace(InputAction.CallbackContext context)
        {
            TogglePlace();
        }

        /// <summary>
        /// Enables placement mode for tile/label. NOTE if is already enabled, edit mode will be enabled.
        /// </summary>
        public void TogglePlace()
        {
            if (tileModeOn)
            {
                if (EditMode == EditModeTypes.tilePlace)
                {
                    ToggleEdit();
                    return;
                }

                EditMode = EditModeTypes.tilePlace;
            }
            else
            {
                if (EditMode == EditModeTypes.labelPlace)
                {
                    ToggleEdit();
                    return;
                }

                EditMode = EditModeTypes.labelPlace;
            }

            DebugOut.Log(this, $"Place enabled.");
        }

        public void ToggleRemove(InputAction.CallbackContext context)
        {
            ToggleRemove();
        }

        /// <summary>
        /// Enables remove mode for tile/label. NOTE if is already enabled, edit mode will be enabled.
        /// </summary>
        public void ToggleRemove()
        {
            if (tileModeOn)
            {
                if (EditMode == EditModeTypes.tileRemove)
                {
                    ToggleEdit();
                    return;
                }

                EditMode = EditModeTypes.tileRemove;
            }
            else
            {
                if (EditMode == EditModeTypes.labelRemove)
                {
                    ToggleEdit();
                    return;
                }

                EditMode = EditModeTypes.labelRemove;
            }

            DebugOut.Log(this, $"Remove enabled.");
        }
        #endregion

        #region selection modes

        /// <summary>
        /// Toggle single select mode. NOTE if is already enabled, it will stay enabled.
        /// </summary>
        public void ToggleSingleSelect(InputAction.CallbackContext context)
        {
            ToggleSingleSelect();
        }

        /// <summary>
        /// Toggle single select mode. NOTE if is already enabled, it will stay enabled.
        /// </summary>
        public void ToggleSingleSelect()
        {
            SelectionMode = SelectModeTypes.singleSelect;
            DebugOut.Log(this, "Single select enabled.");
        }

        /// <summary>
        /// Toggle radial select mode. If disabled, single select mode will be enabled.
        /// </summary>
        public void ToggleRadialSelect(InputAction.CallbackContext context)
        {
            ToggleRadialSelect();
        }

        /// <summary>
        /// Toggle radial select mode. If disabled, single select mode will be enabled.
        /// </summary>
        public void ToggleRadialSelect()
        {
            if (SelectionMode == SelectModeTypes.radialSelect)
            {
                DebugOut.Log(this, "Radial select disabled.");
                ToggleSingleSelect();
                return;
            }

            SelectionMode = SelectModeTypes.radialSelect;
            DebugOut.Log(this, "Radial select enabled.");
        }


        /// <summary>
        /// Toggle area select mode. If disabled, single select mode will be enabled.
        /// </summary>
        public void ToggleAreaSelect(InputAction.CallbackContext context)
        {
            ToggleAreaSelect();
        }

        /// <summary>
        /// Toggle area select mode. If disabled, single select mode will be enabled.
        /// </summary>
        public void ToggleAreaSelect()
        {
            if (SelectionMode == SelectModeTypes.areaSelect)
            {
                DebugOut.Log(this, "Area select disabled.");
                ToggleSingleSelect();
                return;
            }

            SelectionMode = SelectModeTypes.areaSelect;
            DebugOut.Log(this, "Area select enabled.");
        }


        /// <summary>
        /// Toggle draw select mode. If disabled, single select mode will be enabled.
        /// </summary>
        public void ToggleDrawSelect(InputAction.CallbackContext context)
        {
            ToggleDrawSelect();
        }

        /// <summary>
        /// Toggle draw select mode. If disabled, single select mode will be enabled.
        /// </summary>
        public void ToggleDrawSelect()
        {
            if (SelectionMode == SelectModeTypes.drawSelect)
            {
                DebugOut.Log(this, "Draw select disabled.");
                ToggleSingleSelect();
                return;
            }

            SelectionMode = SelectModeTypes.drawSelect;
            DebugOut.Log(this, "Draw select enabled.");
        }

        #endregion

        #region public utility
        public bool IsTilePlaceOn()
        {
            return EditMode == EditModeTypes.tilePlace;
        }
        public bool IsTileEditOn()
        {
            return EditMode == EditModeTypes.tileEdit;
        }

        public bool IsTileRmvOn()
        {
            return EditMode == EditModeTypes.tileRemove;
        }

        public bool IsLabelPlaceOn()
        {
            return EditMode == EditModeTypes.labelPlace;
        }

        public bool IsLabelEditOn()
        {
            return EditMode == EditModeTypes.labelEdit;
        }

        public bool IsTLabelRmvOn()
        {
            return EditMode == EditModeTypes.labelRemove;
        }

        public bool IsEditOn()
        {
            return IsLabelEditOn() || IsTileEditOn();
        }

        public bool IsPlacementOn()
        {
            return IsTilePlaceOn() || IsLabelPlaceOn();
        }

        public bool IsRemoveOn()
        {
            return IsTileRmvOn() || IsTLabelRmvOn();
        }
        #endregion
    }
}
