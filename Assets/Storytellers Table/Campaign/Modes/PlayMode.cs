
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
        private readonly PlayAction _inputMap;

        [SerializeField] private GameObject _runtimeUiInstance;

        public PlayMode(GameObject uiPrefab, Transform uiParentTransform, PlayAction inputMap)
        {
            _uiPrefab = uiPrefab;
            _uiParentTransform = uiParentTransform;
            _inputMap = inputMap;

        }

        void ICampaignMode.Enter()
        {
            // Instantiate UI if it does not exist
            if (_uiPrefab != null && _runtimeUiInstance == null)
                _runtimeUiInstance = Object.Instantiate(_uiPrefab, _uiParentTransform);

            _inputMap.Enable();
            // add call backs to input map...
        }

        void ICampaignMode.Exit()
        {
            _inputMap.Disable();            // disable input for this mode

            if (_runtimeUiInstance != null) // clean up
            {
                Object.Destroy(_runtimeUiInstance);
                _runtimeUiInstance = null;
            }
        }

        void ICampaignMode.UpdateMode()
        {
            if (Keyboard.current.lKey.wasPressedThisFrame)
                Debug.Log("Play Mode - hi!!");
            
            // logic...
        }
    }
}
