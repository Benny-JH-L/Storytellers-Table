
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public abstract class AbstractCamera : MonoBehaviour
{
    public static List<AbstractCamera> cameraList = new();

    [SerializeField] private CameraManager _camManager;
    [SerializeField] protected CameraData cameraData;
    protected Vector3 origin = Vector3.zero;

    /// <summary>
    /// camera instance used.
    /// </summary>
    [SerializeField] protected Camera cam;

    private void Awake()
    {
        Init();
    }

    /// <summary>
    /// Calls in order: Setup(), Configure().
    /// Disables its camera instance and adds it to the `cameraList`.
    /// </summary>
    public void Init()
    {
        _camManager = GetComponentInParent<CameraManager>();
        cam = GetComponent<Camera>();
        cameraData = ScriptableObject.CreateInstance<CameraData>();

        if (cameraData == null)
            cameraData = ScriptableObject.CreateInstance<CameraData>();
        if (cam == null)
            ErrorOut.Throw(this, "cam null!");
        if (_camManager == null)
            ErrorOut.Throw(this, "camera manager null");

        Setup();
        Configure();

        cam.enabled = false;
        cameraList.Add(this);
    }

    public abstract void Setup();
    public abstract void Configure();

    /// <summary>
    /// Remove this camera from the `cameraList` on destruction. 
    /// </summary>
    private void OnDestroy()
    {
        if (cam != null)
            cameraList.Remove(this);
    }

    /// <summary>
    /// Moves the camera's distance into or away (closer or further) to something depending on context.
    /// </summary>
    protected abstract void MoveCameraDistance();

    /// <summary>
    /// Updates the camera when called.
    /// </summary>
    public abstract void UpdateCamera();

    /// <summary>
    /// Enables this camera. Disables all other cameras (done by default if not overriden by parameter).
    /// </summary>
    public void EnableCamera(bool disableAllOtherCams = true)
    {
        if (cam == null)
        {
            Debug.Log("CAMERA IS NULL, UNABLE TO ENABLE!");
            return;
        }
        else if (disableAllOtherCams)
        {
            foreach (AbstractCamera abstractCamera in cameraList)
            {
                //abstractCamera.cam.enabled = false;
                abstractCamera.DisableCamera();
            }

        }
        //string s = this is OrbitCam ? "ORBIT" : "MAP";
        //Debug.Log($"enabling camera: {s}");
        cam.enabled = true;
    }

    /// <summary>
    /// Disable's this camera.
    /// </summary>
    public void DisableCamera()
    {
        cam.enabled = false;
    }

    /// <summary>
    /// Toggles if this camrea is enabled or disabled. If it's enabled, sets itself to disabled, and viseversa.
    /// </summary>
    public void ToggleEnable()
    {
        cam.enabled = !cam.enabled;
    }

    /// <summary>
    /// Returns a boolean indicating whether or not this camrea is enabled.
    /// </summary>
    /// <returns>Boolean. true, camera is enabled. false otherwise.</returns>
    public bool IsCamEnabled()
    {
        return cam.enabled;
    }

    /// <summary>
    /// Try not to call this unless absolutely necessary :).
    /// </summary>
    /// <returns></returns>
    public Camera GetCamera()
    {
        return cam;
    }

    /// <summary>
    /// Enables/Disables the orthographoc view.
    /// True: enables orthographics view.
    /// False: enables perspective view.
    /// </summary>
    /// <param name="enableOrtho"></param>
    public void EnableOrthographicView(bool enableOrtho = true)
    {
        cam.orthographic = enableOrtho;
    }

    /// <summary>
    /// Zoom's the camera's Perspective FOV, or Ortho size with respect to mouse scroll input.
    /// </summary>
    protected void ZoomCamera()
    {
        float scrollDelta = Mouse.current.scroll.ReadValue().y; // zoom in +1, zoom out -1
        //Debug.Log("scroll data:"+scrollDelta);

        if (Mathf.Abs(scrollDelta) > 0.01f)
        {
            if (cam.orthographic)
            {
                cam.orthographicSize -= scrollDelta * (cameraData.zoomSpeed * 0.01f);
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, cameraData.minOrthoSize, cameraData.maxOrthoSize);
            }
            else
            {
                cam.fieldOfView -= scrollDelta * cameraData.zoomSpeed * Time.deltaTime;
                cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, cameraData.minZoom, cameraData.maxZoom);
            }
        }
    }

    /// <summary>
    /// Return's the world location (Vector3) of where the mouse is over, using this camera.
    /// Otherwise, return Vector3.zero.
    /// </summary>
    /// <returns>Vector3</returns>
    public Vector3 GetMousePosInWorld()
    {
        Ray ray = cam.ScreenPointToRay(_camManager.mouseTracker.GetMousePosInScreen());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            //DebugOut.Log(this, $"hit pos: {hit.point}");
            return hit.point;
        }
        return Vector3.zero;
    }
}
