//using System;
//using UnityEngine;
//using UnityEngine.InputSystem;

//[System.Serializable]
//public class CameraManager : MonoBehaviour
//{
//    // will be on the gameobject
//    //public GameEventListener onSelectedObject;
//    //public GameEventListener onCameraProjectionChange;
//    //private static string _cameraDebugStart = "Camera Manager | ";
//    //public static CameraData cameraData;

//    private MapCam _mapCam;
//    private OrbitCam _orbitCam; // actually used | for game object
//    private AbstractCamera _currentCam;
//    public MouseTracker mouseTracker;

//    //public bool _isUIFocused;   // only public so inspector can see it
//    //private bool _isGameScreenFocused; // most likely do not need anymore

//    private void Awake()
//    {
//        _orbitCam = GetComponentInChildren<OrbitCam>();
//        _mapCam = GetComponentInChildren<MapCam>();

//        if (_mapCam == _orbitCam)
//            ErrorOut.Throw(this, " - Awake() - Map and Orbit cam cannot be the same object!");

//        if (mouseTracker == null)
//            ErrorOut.Throw(this, "mouseTracker null");
//    }

//    // Start is called once before the first execution of Update after the MonoBehaviour is created
//    void Start()
//    {
//        //_isUIFocused = false;
//        _mapCam.EnableCamera();
//        _currentCam = _mapCam;

//        if (_currentCam != null && _currentCam.IsCamEnabled())
//        {
//            DebugOut.Log(this, "Map cam enabled!");
//        }
//    }

//    // Update is called once per frame
//    void Update()
//    {
//        // idk what the reason is for this...
//        //if (_isUIFocused)   // Do not allow Camera adjustments while interacting with the UI
//        //{
//        //    //Debug.Log($"{_cameraDebugStart}UI is focused");
//        //    return;
//        //}

//        UpdateCameras();
//    }

//    /// <summary>
//    /// Calls update functions of the map and orbit cameras (AbstractCamera).
//    /// </summary>
//    private void UpdateCameras()
//    {
//        if (Keyboard.current.backquoteKey.wasPressedThisFrame) // key: '`'
//        {
//            ToggleCam();
//        }

//        if (_mapCam.IsCamEnabled())
//        {
//            _mapCam.UpdateCamera();
//        }
//        else if (_orbitCam.IsCamEnabled())
//        {
//            _orbitCam.UpdateCamera();
//        }
//    }

//    /// <summary>
//    /// Toggles between the map cam and orbit cam.
//    /// </summary>
//    private void ToggleCam()
//    {
//        _mapCam.ToggleEnable();
//        _orbitCam.ToggleEnable();

//        foreach (AbstractCamera abstractCamera in AbstractCamera.cameraList)
//        {
//            if (abstractCamera.IsCamEnabled())
//            {
//                _currentCam = abstractCamera;
//                break;
//            }
//        }

//        if (_mapCam.IsCamEnabled())
//            //Debug.Log($"{_cameraDebugStart}Map cam enabled");
//            DebugOut.Log(this, "Map cam enabled");
//        else if (_orbitCam.IsCamEnabled())
//            //Debug.Log($"{_cameraDebugStart}Orbit cam enabled");
//            DebugOut.Log(this, "Orbit cam enabled");

//    }

//    /// <summary>
//    /// Returns a tuple with the `gameObject` the mouse is over and the mouse's location in the world.
//    /// </summary>
//    /// <returns>
//    /// Tuple(GameObject, Vector3)
//    /// Item 1: GameObject the mouse is over. Is Null if the mouse is not over a GameObject.
//    /// Item 2: Location of where the mouse is over in the world. Is Null if the mouse is not over a GameObject.
//    /// </returns>
//    public Tuple<GameObject, Vector3> GetGameObjectAtMousePos()
//    {
//        //Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue()); // works with camera's with the tag "MainCamera"
        
//        //string s = _currentCam is OrbitCam ? "ORBIT" : "MAP";
//        //Debug.Log($"current cam: {s}");

//        Ray ray = _currentCam.GetCamera().ScreenPointToRay(mouseTracker.GetMousePosInScreen());
//        if (Physics.Raycast(ray, out RaycastHit hit))
//        {
//            //Debug.Log("Camera | Mouse hit <" + hit.collider.gameObject + "> at " + hit.point);
//            return new Tuple<GameObject, Vector3>(hit.collider.gameObject, hit.point);
//        }

//        return null;
//    }

//    /// <summary>
//    /// Return's the world location (Vector3) of where the mouse is over.
//    /// Otherwise, return Vector3.zero.
//    /// </summary>
//    /// <returns>Vector3</returns>
//    public Vector3 GetMousePosInWorld()
//    {
//        return _currentCam.GetMousePosInWorld();
//    }

//    public void OnSelectedObjectChanged(Component sender, object data)
//    {
//        DebugOut.Log(this, "OnSelectedObjectChanged");
//        if (data is ChangedObject changedObject)
//        {
//            // enable map camera
//            if (changedObject.newSelectedObj == null)
//            {
//                _mapCam.EnableCamera();
//                _currentCam = _mapCam;
//                return;
//            }
//            // enable orbit camera
//            DebugOut.Log(this, "Switching to orbit cam");
//            _orbitCam.EnableCamera(changedObject.newSelectedObj.appearanceGameObj); // orbit target is the GameObject `appearanceGameObj`
//            _currentCam = _orbitCam;
//        }
//    }

//    //public void OnSelectedObject(Component sender, object data)
//    //{
//    //    if (data is TTRPG_SceneObjectBase)   // maybe have a condition to check whether or not we switch to player or go to map cam... (Eagel or creature view to moving..)
//    //    {
//    //        //Debug.Log($"{_cameraDebugStart}Switching to orbit cam");
//    //        DebugOut.Log(this, "Switching to orbit cam");

//    //        TTRPG_SceneObjectBase obj = (TTRPG_SceneObjectBase)data;
//    //        _orbitCam.EnableCamera(obj.appearanceGameObj);
//    //        _currentCam = _orbitCam;
//    //    }
//    //}

//    //public void OnDeselectObject(Component sender, object data)
//    //{
//    //    _mapCam.EnableCamera();
//    //    _currentCam = _mapCam;
//    //}

//    /// <summary>
//    /// Gets the active camera in the scene.
//    /// </summary>
//    /// <returns></returns>
//    public AbstractCamera GetCurrentCamera()
//    {
//        return _currentCam;
//    }

//    //public void OnUIFocued(Component comp, object data)
//    //{
//    //    if (data is bool r)
//    //        _isUIFocused = r;
//    //}

//    //public void OnGameScreenFocused(Component comp, object data) // most likely do not need anymore
//    //{
//    //    if (data is bool r)
//    //        _isGameScreenFocused = r;
//    //}

//    public void OnGeneralObjectDestroyed(Component comp, object data)   // idk if i'll use this
//    {
//        // if the orbit cam is enabled and the object is destroyed, switch to map cam.
//        if (_orbitCam.IsCamEnabled())
//        {
//            ToggleCam();
//        }
//    }
//}
