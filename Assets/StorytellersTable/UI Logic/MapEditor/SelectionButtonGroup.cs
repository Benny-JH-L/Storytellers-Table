
using StorytellersTable.Utility.Log;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StorytellersTable.UiLogic
{
    public class SelectionButtonGroup : ButtonGroup
    {
        // Assigned in the editor
        [SerializeField] public ST_Button singleSelBtn;
        [SerializeField] public ST_Button areaSelBtn;
        [SerializeField] public ST_Button radialSelBtn;
        [SerializeField] public ST_Button drawSelBtn;

        public override void Configure()
        {
            DebugOut.Log(this, "Configure()");

            // Configure the normal, highlight, pressed, select, .., colors, fade duration etc.
        }

        public override void Setup()
        {
            ST_Button[] btns = GetComponentsInChildren<ST_Button>();

            if (singleSelBtn == null)
                singleSelBtn = btns[0];
            if (areaSelBtn == null)
                areaSelBtn = btns[1];
            if (radialSelBtn == null)
                radialSelBtn = btns[2];
            if (drawSelBtn == null)
                drawSelBtn = btns[3];

            foreach (ST_Button b in btns)
            {
                buttons[b.name] = b;
            }
        }
    }
}
