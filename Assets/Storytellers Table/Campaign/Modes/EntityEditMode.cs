
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
        private readonly EntityEditAction _inputMap;

        [SerializeField] private GameObject _runtimeUiInstance;

        public EntityEditMode(GameObject uiPrefab, Transform uiParentTransform, EntityEditAction inputMap)
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
                Debug.Log("Entity edit - hi!!");

            // logic...
        }
    }
}
