
using UnityEngine;
using UnityEngine.InputSystem;

namespace StorytellersTable.Campaign.Modes
{
    /// <summary>
    /// Encapsulates behavior for constructing attributes, inventory items, and customization modules on entities.
    /// </summary>
    public class EntityEditMode : ICampaignMode
    {
        private readonly GameObject _uiPrefab;
        private readonly Transform _uiParentTransform;
        //private readonly InputActionMap _inputMap;

        void ICampaignMode.Enter()
        {
            throw new System.NotImplementedException();
        }

        void ICampaignMode.Exit()
        {
            throw new System.NotImplementedException();
        }

        void ICampaignMode.UpdateMode()
        {
            throw new System.NotImplementedException();
        }
    }
}
