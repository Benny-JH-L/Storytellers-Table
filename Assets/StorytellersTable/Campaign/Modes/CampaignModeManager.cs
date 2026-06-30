
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using StorytellersTable.Utility.Log;

namespace StorytellersTable.Campaign.Modes
{
    /// <summary>
    /// Central driver of state machine (campaign mode state), lifecycle, configurations and mode transitions.
    /// </summary>
    [DisallowMultipleComponent]
    public class CampaignModeManager : MonoBehaviour
    {
        public static CampaignModeManager Instance { get; private set; }

        [Header("UI Canvas Hierarchy Configurations")]
        [SerializeField] private Transform _uiCanvasRoot;   // needs to be a `Canvas` instance

        [Header("Mode Specific UI Prefabs")]
        [SerializeField] private GameObject _mapEditUiPrefab;
        [SerializeField] private GameObject _entityEditUiPrefab;
        [SerializeField] private GameObject _playUiPrefab;

        [Header("Action Inputs")]
        [SerializeField] private PlayAction _playActions;
        [SerializeField] private MapEditAction _mapEditActions;
        [SerializeField] private EntityEditAction _entityEditActions;
        [SerializeField] private ModeManagerAction _modeManagerActions;

        [Header("Other")]
        

        private Dictionary<CampaignModeType, ICampaignMode> _modes; // map the campaign type to an instance to manage its logic
        private ICampaignMode _currentMode;
        
        public CampaignModeType CurrentModeType { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            DebugOut.Log(this, "Awake()");

            Instance = this;

            _playActions = new PlayAction();
            _mapEditActions = new MapEditAction();
            _entityEditActions = new EntityEditAction();
            _modeManagerActions = new ModeManagerAction();

            ValidateDependencies();
            InitializeStateMachine();
            InitializeManagerActions();
        }

        private void OnEnable()
        {
            SwitchMode(CampaignModeType.MapEdit); // set to play when its implemented
            _modeManagerActions.Enable();
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
                ErrorOut.Log(this, $"Failed to transition. Mode '{switchToMode}' is not registered.");
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

            DebugOut.Log(this, $"Successfully transitioned mode to: {switchToMode}");
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
                {
                    CampaignModeType.EntityEdit,
                    new EntityEditMode(_entityEditUiPrefab, _uiCanvasRoot, _entityEditActions)
                },
                {
                    CampaignModeType.Play,
                    new PlayMode(_playUiPrefab, _uiCanvasRoot, _playActions)
                }
            };
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

        private void InitializeManagerActions()
        {
            _modeManagerActions.bind.CycleMode.performed += CycleMode;
        }

        /// <summary>
        /// Cycles the campaign mode (Circular cycling)
        /// </summary>
        /// <param name="context"></param>
        private void CycleMode(InputAction.CallbackContext context)
        {
            switch (CurrentModeType)
            {
                case CampaignModeType.Play:
                    SwitchMode(CampaignModeType.MapEdit);
                    break;
                case CampaignModeType.MapEdit:
                    SwitchMode(CampaignModeType.EntityEdit);
                    break;
                case CampaignModeType.EntityEdit:
                    SwitchMode(CampaignModeType.Play);
                    break;
            }

        }

        private void OnDestroy()
        {
            // clean up when the manager is destroyed.
            _currentMode?.Exit();
            _modes.Clear();
        }
    }
}
