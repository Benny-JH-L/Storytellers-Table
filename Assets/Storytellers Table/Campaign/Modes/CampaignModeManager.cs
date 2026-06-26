
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StorytellersTable.Campaign.Modes
{
    /// <summary>
    /// Central driver of state machine (campaign mode state), lifecycle, configurations and mode transitions.
    /// </summary>
    public class CampaignModeManager : MonoBehaviour
    {
        [Header("UI Canvas Hierarchy Configurations")]
        [SerializeField] private Transform _uiCanvasRoot;

        [Header("Mode Specific UI Prefabs")]
        [SerializeField] private GameObject _mapEditUiPrefab;
        [SerializeField] private GameObject _entityEditUiPrefab;
        [SerializeField] private GameObject _playUiPrefab;

        [SerializeField] private InputActionAsset _playActions; // key binds to quickly swap between campaign modes, do what i did for the camera
        [SerializeField] private MapEditAction _mapEditActions;
        [SerializeField] private InputActionAsset _entityEditActions; // key binds to quickly swap between campaign modes, do what i did for the camera

        [Header("Temporary | Testing only")]
        [SerializeField] private MapBase map;
        [SerializeField] private Material material;
        [SerializeField] private Material ghostMaterial;

        private Dictionary<CampaignModeType, ICampaignMode> _modes; // map the campaign type to an instance to manage its logic
        private ICampaignMode _currentMode;

        public CampaignModeType CurrentModeType { get; private set; }

        private void Awake()
        {
            _mapEditActions = new MapEditAction();

            ValidateDependencies();
            InitializeStateMachine();
        }

        private void OnEnable()
        {
            SwitchMode(CampaignModeType.MapEdit); // set to play when its implemented
        }

        private void Update() // contains this Update() for now, will have a seprate `UpdateMode` function similar done to the `modes`
        {
            _currentMode?.UpdateMode(); 
        }

        /// <summary>
        /// Triggers mode switch to parameter type.
        /// </summary>
        /// <param name="switchToMode">The campaign mode to swap to.</param>
        public void SwitchMode(CampaignModeType switchToMode)
        {
            if (_modes == null || !_modes.TryGetValue(switchToMode, out ICampaignMode targetMode))
            {
                Debug.LogError($"[CampaignModeManager] Failed to transition. Mode '{switchToMode}' is not registered.");
                return;
            }

            if (_currentMode == targetMode)
                return;

            // clean up current mode (as long its not null)
            _currentMode?.Exit();

            // set new values
            _currentMode = targetMode;
            CurrentModeType = switchToMode;

            // set up
            _currentMode.Enter();

            Debug.Log($"[CampaignModeManager] Successfully transitioned mode to: {switchToMode}");
        }

        private void InitializeStateMachine()
        {
            // Dependencies are strictly injected down into states via constructor composition
            _modes = new Dictionary<CampaignModeType, ICampaignMode>
            {
                {
                    CampaignModeType.MapEdit,
                    new MapEditMode(_mapEditUiPrefab, _uiCanvasRoot, _mapEditActions)
                },
                //{
                //    CampaignModeType.EntityEdit,
                //    new EntityEditMode(_entityEditUiPrefab, _uiCanvasRoot, _entityEditActions?.FindActionMap("EntityEditActions"))
                //},
                //{
                //    CampaignModeType.Play,
                //    new PlayMode(_playUiPrefab, _uiCanvasRoot, _playActions?.FindActionMap("PlayActions"))
                //}
            };

            // TEMPORARY
            MapEditMode mapEditMpde = (MapEditMode)_modes[CampaignModeType.MapEdit];
            mapEditMpde.map = map;
            mapEditMpde.placedMaterial = material;
            mapEditMpde.ghostMaterial = ghostMaterial;

            Debug.Log($"_mode size: {_modes.Count}");
        }

        private void ValidateDependencies()
        {
            if (_uiCanvasRoot == null) 
                throw new ArgumentNullException(nameof(_uiCanvasRoot), "UI Canvas Root must be assigned in inspector.");
            if (_mapEditUiPrefab == null) 
                throw new ArgumentNullException(nameof(_mapEditUiPrefab));
            if (_entityEditUiPrefab == null) 
                throw new ArgumentNullException(nameof(_entityEditUiPrefab));
            if (_playUiPrefab == null) 
                throw new ArgumentNullException(nameof(_playUiPrefab));
        }

        private void OnDestroy()
        {
            // clean up when the manager is destroyed.
            _currentMode?.Exit();
            _modes.Clear();
        }
    }
}
