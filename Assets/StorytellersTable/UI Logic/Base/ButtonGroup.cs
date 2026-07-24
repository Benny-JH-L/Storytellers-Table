
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StorytellersTable.UiLogic
{

    public abstract class ButtonGroup : CustomUIComponent
    {
        // Stores buttons in the button group using an identifier
        public readonly Dictionary<string, ST_Button> buttons = new();
        // scriptahble object to configure font and what not

    }
}
