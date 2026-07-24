
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace StorytellersTable.UiLogic
{
    public abstract class ToggleButtonBase : CustomUIComponent
    {
        public abstract void AddListener(UnityEngine.Events.UnityAction call);
        public abstract void UpdateAppearance();
    }
}
