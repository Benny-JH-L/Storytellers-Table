
//using UnityEngine;
//using UnityEngine.InputSystem;

//[System.Serializable]
//public class MapCam : AbstractCamera
//{
//    private static string _debugStart = "MapCam | ";
    
//    [Header("Mouse World Position")]
//    [SerializeField] private Vector3 _currWorldPos = Vector3.positiveInfinity;
//    [SerializeField] private Vector3 _prevWorldPos = Vector3.positiveInfinity;
//    //[SerializeField] private bool _isDragging = false;

//    public override void Setup()
//    {
//        // nothing...
//    }

//    public override void Configure()
//    {
//        if (cameraData == null)
//            Debug.LogError($"{_debugStart}CAMERA DATA IS NULL");
//    }

//    public override void UpdateCamera()
//    {
//        // Camera zoom
//        if (Keyboard.current.shiftKey.isPressed)
//            ZoomCamera();           // check for zoom input 
//        else
//            MoveCameraDistance();   // check for camera movement (along y-axis)

//        MouseCameraPan();
//        KeyboardCameraPan();
//    }

//    /// <summary>
//    /// Moves the map camera into the screen or away from the screen, with respect to mouse scroll input. 
//    /// Ie changes the y value of the camera.
//    /// </summary>
//    protected override void MoveCameraDistance()
//    {
//        float scrollDelta = Mouse.current.scroll.ReadValue().y; // zoom in +1, zoom out -1
//        Vector3 pos = cam.transform.position;
//        pos.y -= (scrollDelta * cameraData.mapZoomInOutSensitivity * Time.deltaTime);
//        cam.transform.position = pos;
//    }

//    /// <summary>
//    /// When the middle mouse button is pressed, pans (moves along the xz-plane) the camera based on mouse movement.
//    /// </summary>
//    private void MouseCameraPan()
//    {
//        // Check camera panning
//        if (Mouse.current.middleButton.isPressed)
//        {
//            _currWorldPos = GetMousePosInWorld();   // set current position

//            // compute delta vector and move camera when `_prevWorldPos` has been set.
//            if (_prevWorldPos.magnitude != float.PositiveInfinity && _currWorldPos.magnitude != float.PositiveInfinity) //[doesn't fix the jittering from (_prevWorldPos = _currWorldPos) ]: && _prevWorldPos != _currWorldPos)
//            {
//                //Debug.Log($"prevpos: {_prevWorldPos}\ncurrpos: {_currWorldPos}");

//                Vector3 delta = _prevWorldPos - _currWorldPos;  // compute delta vector from two positions
//                delta.y = 0f;                                   // ignore y-axis change
//                transform.position += delta;                    // move the camera by delta vector
//            }
//            _prevWorldPos = GetMousePosInWorld();   // set previous position
//            //_prevWorldPos = _currWorldPos;    // causes major jittering
//        }
//        // Reset positions when middle mouse button is not pressed
//        else
//        {
//            _currWorldPos = Vector3.positiveInfinity;
//            _prevWorldPos = Vector3.positiveInfinity;
//        }

//        //// Detect when middle mouse is first pressed
//        //if (Mouse.current.middleButton.wasPressedThisFrame)
//        //{
//        //    _prevWorldPos = GetMousePosInWorld();
//        //    _isDragging = true;
//        //}
//        //// While middle mouse is held down
//        //else if (Mouse.current.middleButton.isPressed && _isDragging)
//        //{
//        //    Vector3 currentMouseWorldPos = GetMousePosInWorld();

//        //    // Calculate the distance the mouse moved in world space
//        //    Vector3 delta = _prevWorldPos - currentMouseWorldPos;

//        //    // Move camera by that delta (negative because we want to move opposite to mouse)
//        //    // This creates the "dragging the world" feel
//        //    transform.position += new Vector3(delta.x, 0f, delta.z);

//        //    // Update previous position for next frame
//        //    _prevWorldPos = GetMousePosInWorld();
//        //}
//        //// When button is released
//        //else if (Mouse.current.middleButton.wasReleasedThisFrame)
//        //{
//        //    _isDragging = false;
//        //}
//    }

//    /// <summary>
//    /// Pans (moves along the xz-plane) the camera based on keyboard input, using the `wasd` keys.
//    /// </summary>
//    private void KeyboardCameraPan()
//    {
//        MovementValue movementValue = new(cameraData.mapMoveSensitivity);

//        // use `wasd` to move camera
//        // Check if any of the movement keys are being held
//        if (Keyboard.current.wKey.isPressed)
//        {
//            movementValue.MoveNorth();
//        }
//        else if (Keyboard.current.sKey.isPressed)
//        {
//            movementValue.MoveSouth();
//        }

//        if (Keyboard.current.aKey.isPressed)
//        {
//            movementValue.MoveWest();
//        }
//        else if (Keyboard.current.dKey.isPressed)
//        {
//            movementValue.MoveEast();
//        }

//        transform.position += movementValue.GetMovement();
//    }

//    // DEPRECATED
//    ///// <summary>
//    ///// [DEPRECATED] Takes in a `MovementValue` and uses it to move the map camera's position.
//    ///// </summary>
//    ///// <param name="moveBy"></param>
//    //public void MoveMapCamBy(MovementValue moveBy)
//    //{
//    //    cam.transform.position += moveBy.GetMovement();
//    //}
//}