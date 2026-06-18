
//using UnityEngine;
//using UnityEngine.InputSystem;

//[System.Serializable]
//public class OrbitCam : AbstractCamera
//{
//    private static string _debugStart = "OrbitCam | ";

//    /// <summary>
//    /// Variable to set when a Orbit Camera is going to be used
//    /// </summary>
//    //private Transform orbitTarget;
//    [SerializeField]private GameObject orbitTarget;
    
//    private float currentRotatX;   // theta
//    private float currentRotatY;   // phi
//    private float orbitDistance;   // how far camera is from object

//    public override void Setup()
//    {
//        orbitTarget = null;
//        currentRotatX = 0;
//        currentRotatY = 0;
//        orbitDistance = 0;
//    }

//    public override void Configure()
//    {
//        if (cameraData == null)
//            Debug.Log($"{_debugStart}CAMERA DATA IS NULL");

//        currentRotatX = cameraData.initXRotatation;
//        currentRotatY = cameraData.initYRotatation;
//        orbitDistance = cameraData.initOrbitDistance;
//    }

//    /// <summary>
//    /// Updates Orbit camera data; zoom (ortho, perspective), orbit distance, rotations (orbit), and positioning.
//    /// </summary>
//    public override void UpdateCamera()
//    {
//        if (Keyboard.current.shiftKey.isPressed)    // check zoom 
//            ZoomCamera(); 
//        else
//            MoveCameraDistance();                   // otherwise, check the orbit distance from `orbitTarget`

//        if (!Mouse.current.middleButton.isPressed)  // don't orbit cam when middle mouse is not held down, but update camera position relative to selected object's position
//        {
//            Vector3 direction = new Vector3(0, 0, -orbitDistance);
//            cam.transform.position = (orbitTarget != null ? orbitTarget.transform.position : origin) + (cam.transform.rotation * direction);
//            //UpdateOrbitCamOnce();
//            return;
//        }

//        OrbitCamLogic();
//    }

//    /// <summary>
//    /// Enables this camera. Disables all other cameras (done by default if not overriden by parameter).
//    /// orbitTarget: the target the orbit camera will look at.
//    /// </summary>
//    /// <param name="orbitTarget"></param>
//    public void EnableCamera(GameObject orbitTarget)
//    {
//        this.orbitTarget = orbitTarget;
//        EnableCamera();
//        UpdateOrbitCamOnce();
//    }

//    /// <summary>
//    /// Logic for orbit camera movement.
//    /// </summary>
//    private void OrbitCamLogic()
//    {
//        float scrollDelta = Mouse.current.scroll.ReadValue().y; // zoom in +1, zoom out -1

//        Vector2 lookDelta = Mouse.current.delta.ReadValue();
//        currentRotatX += lookDelta.x * cameraData.orbitSens * Time.deltaTime;
//        currentRotatY -= lookDelta.y * cameraData.orbitSens * Time.deltaTime;
//        currentRotatY = Mathf.Clamp(currentRotatY, cameraData.orbitMinVerticalClamp, cameraData.orbitMaxVerticalClamp);

//        Quaternion rotation = Quaternion.Euler(currentRotatY, currentRotatX, 0);
//        Vector3 direction = new Vector3(0, 0, -orbitDistance);
//        Vector3 position = ((orbitTarget != null) ? orbitTarget.transform.position : origin) + rotation * direction;

//        cam.transform.position = position;
//        cam.transform.rotation = rotation;
//    }

//    /// <summary>
//    /// Moves the orbit camera distance into the `orbitTarget` or away from the `orbitTarget`,
//    /// with respect to mouse scroll input. 
//    /// </summary>
//    protected override void MoveCameraDistance()
//    {
//        float scrollDelta = Mouse.current.scroll.ReadValue().y; // zoom in +1, zoom out -1
//        orbitDistance -= scrollDelta * cameraData.orbitDistanceZoomSens;
//        orbitDistance = Mathf.Clamp(orbitDistance, cameraData.orbitMinDist, cameraData.orbitMaxDist);
//    }

//    /// <summary>
//    /// Calls `OrbitCamLogic()` once.
//    /// </summary>
//    private void UpdateOrbitCamOnce()
//    {
//        OrbitCamLogic();
//    }

//}