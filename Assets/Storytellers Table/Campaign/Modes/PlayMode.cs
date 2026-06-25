
using UnityEngine;
using UnityEngine.InputSystem;

namespace StorytellersTable.Campaign.Modes
{
    /// <summary>
    /// Encapsulates behavior during execution of session logic, combat turns, initiative trackers, and real-time navigation.
    /// </summary>
    public class PlayMode : ICampaignMode
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
