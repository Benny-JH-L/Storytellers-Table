
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StorytellersTable.UiLogic
{

    public abstract class ButtonGroup : CustomUIComponent
    {
        // Stores buttons in the button group using an identifier
        public readonly Dictionary<string, ST_Button> buttons = new();

        public override void Configure()
        {
            throw new System.NotImplementedException();
        }

        public override void Setup()
        {
            throw new System.NotImplementedException();
        }
    }
}
