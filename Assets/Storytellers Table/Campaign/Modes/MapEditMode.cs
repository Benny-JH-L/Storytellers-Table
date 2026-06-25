
using UnityEngine;
using UnityEngine.InputSystem;

namespace StorytellersTable.Campaign.Modes
{
    /// <summary>
    /// Encapsulates behavior while modifying the map tiles; layout coordinates, layered tile placement, and geometry.
    /// </summary>
    public class MapEditMode : ICampaignMode
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
