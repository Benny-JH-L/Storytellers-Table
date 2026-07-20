
using UnityEngine;
using UnityEngine.UI;

namespace StorytellersTable.UiLogic
{
    /// <summary>
    /// Custom button for the Storyteller's Table.
    /// </summary>
    public class ST_Button : CustomUIComponent
    {
        [SerializeField] public Button button;

        public override void Configure()
        {
        }

        public override void Setup()
        {
            if (button == null)
            {
                Button btn = GetComponentInChildren<Button>(true);
                if (btn != null)
                    button = btn;
                else
                    button = gameObject.AddComponent<Button>();
            }
        }
    }
}
