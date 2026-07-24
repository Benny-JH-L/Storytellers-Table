
using StorytellersTable.Utility.Log;
using UnityEngine;

namespace StorytellersTable.UiLogic
{
    public class EditModeButtonGroup : ButtonGroup
    {
        // Assigned in the editor
        [SerializeField] public ST_Button removeBtn;
        [SerializeField] public ST_Button placeBtn;
        [SerializeField] public ST_Button editBtn;

        public override void Configure()
        {
            DebugOut.Log(this, "Configure()");

            // Configure the normal, highlight, pressed, select, .., colors, fade duration etc.
        }

        public override void Setup()
        {
            ST_Button[] btns = GetComponentsInChildren<ST_Button>();

            if (removeBtn == null)
                removeBtn = btns[0];
            if (placeBtn == null)
                placeBtn = btns[1];
            if (editBtn == null)
                editBtn = btns[2];

            foreach (ST_Button b in btns)
            {
                buttons[b.name] = b;
            }
        }
    }
}
