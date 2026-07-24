
using StorytellersTable.Campaign.Modes;
using StorytellersTable.Utility.Log;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace StorytellersTable.UiLogic
{
    public class TileLabelToggleButton : ToggleButtonBase
    {
        [SerializeField] private ST_Button toggleButton;
        [SerializeField] private HorizontalLayoutGroup apperance;    // temporary thing to show state

        public override void AddListener(UnityAction call)
        {
            toggleButton.button.onClick.AddListener(call);
            toggleButton.button.onClick.AddListener(UpdateAppearance);
        }

        public override void Configure()
        {
            DebugOut.Log(this, "configure()");

        }

        public override void Setup()
        {
            ST_Button[] btns = GetComponentsInChildren<ST_Button>();

            if (toggleButton == null)
                toggleButton = btns[0];

            if (apperance == null)
                apperance = GetComponentsInChildren<HorizontalLayoutGroup>()[0];
        }

        public override void UpdateAppearance()
        {
            ModeContainer modeContainer = MapEditorContainer.instance.editModes;
            Vector3 currPos = apperance.transform.position;
            
            // tile mode - left
            if (modeContainer.IsTileModeOn() && apperance.reverseArrangement)
            {
                // slide to the left
                apperance.reverseArrangement = false;
            }
            // label mode - right
            else
            {
                // slide to the right
                apperance.reverseArrangement = true;
            }

        }
    }
}
