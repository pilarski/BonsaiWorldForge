/*
   Based on the simple Unity camera controller script
   from the URP project template.

   All game-specific additions to the standard template copyright
   2022 Patrick M. Pilarski
   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at
       http://www.apache.org/licenses/LICENSE-2.0
   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
 */

// This file is responsible for player control in mouse-keyboard play
// and for managing collision detection with the environment and changing
// lighting/fog in response to environmental location.


#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

using UnityEngine;

namespace UnityTemplateProjects
{
    public class SimpleCameraController : MonoBehaviour
    {

        public SwitchList SwitchList;
        public Interact InteractScript;
        public GameObject Sun;
        public GameObject PlayerLight;
        public AudioClip AboveWaterAudio;
        public AudioClip BelowWaterAudio;
        public bool IsUnderwater;
        public bool InCave;
        public float TerrainCollisionThreshold = 1f;
        public float CaveCollisionThreshold = 5f;
        public float PlayPerimiter = 150f;

        class CameraState
        {
            public float yaw;
            public float pitch;
            public float roll;
            public float x;
            public float y;
            public float z;

            public void SetFromTransform(Transform t)
            {
                pitch = t.eulerAngles.x;
                yaw = t.eulerAngles.y;
                roll = t.eulerAngles.z;
                x = t.position.x;
                y = t.position.y;
                z = t.position.z;
            }

            public void Translate(Vector3 translation)
            {
                Vector3 rotatedTranslation = Quaternion.Euler(pitch, yaw, roll) * translation;

                x += rotatedTranslation.x;
                y += rotatedTranslation.y;
                z += rotatedTranslation.z;
            }

            public void RawTranslate(Vector3 translation)
            {
                x += translation.x;
                y += translation.y;
                z += translation.z;
            }

            public void LerpTowards(CameraState target, float positionLerpPct, float rotationLerpPct)
            {
                yaw = Mathf.Lerp(yaw, target.yaw, rotationLerpPct);
                pitch = Mathf.Lerp(pitch, target.pitch, rotationLerpPct);
                roll = Mathf.Lerp(roll, target.roll, rotationLerpPct);
                
                x = Mathf.Lerp(x, target.x, positionLerpPct);
                y = Mathf.Lerp(y, target.y, positionLerpPct);
                z = Mathf.Lerp(z, target.z, positionLerpPct);
            }

            public void UpdateTransform(Transform t)
            {
                t.eulerAngles = new Vector3(pitch, yaw, roll);
                t.position = new Vector3(x, y, z);
            }
        }

        const float k_MouseSensitivityMultiplier = 0.01f;

        CameraState m_TargetCameraState = new CameraState();
        CameraState m_InterpolatingCameraState = new CameraState();

        [Header("Movement Settings")]
        [Tooltip("Exponential boost factor on translation, controllable by mouse wheel.")]
        public float boost = 3.5f;

        [Tooltip("Time it takes to interpolate camera position 99% of the way to the target."), Range(0.001f, 1f)]
        public float positionLerpTime = 0.2f;

        [Header("Rotation Settings")]
        [Tooltip("Multiplier for the sensitivity of the rotation.")]
        public float mouseSensitivity = 60.0f;

        public float boostRotation = 1.0f;

        [Tooltip("X = Change in mouse position.\nY = Multiplicative factor for camera rotation.")]
        public AnimationCurve mouseSensitivityCurve = new AnimationCurve(new Keyframe(0f, 0.5f, 0f, 5f), new Keyframe(1f, 2.5f, 0f, 0f));

        [Tooltip("Time it takes to interpolate camera rotation 99% of the way to the target."), Range(0.001f, 1f)]
        public float rotationLerpTime = 0.01f;

        [Tooltip("Whether or not to invert our Y axis for mouse input to rotation.")]
        public bool invertY = false;

#if ENABLE_INPUT_SYSTEM
        InputAction movementAction;
        InputAction verticalMovementAction;
        InputAction lookAction;
        InputAction boostFactorAction;
        bool        mouseRightButtonPressed;

        void Start()
        {
            
            var map = new InputActionMap("Simple Camera Controller");

            lookAction = map.AddAction("look", binding: "<Mouse>/delta");
            movementAction = map.AddAction("move", binding: "<Gamepad>/leftStick");
            verticalMovementAction = map.AddAction("Vertical Movement");
            boostFactorAction = map.AddAction("Boost Factor", binding: "<Mouse>/scroll");

            lookAction.AddBinding("<Gamepad>/rightStick").WithProcessor("scaleVector2(x=15, y=15)");
            movementAction.AddCompositeBinding("Dpad")
                .With("Up", "<Keyboard>/w")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/s")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/a")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/d")
                .With("Right", "<Keyboard>/rightArrow");
            verticalMovementAction.AddCompositeBinding("Dpad")
                .With("Up", "<Keyboard>/pageUp")
                .With("Down", "<Keyboard>/pageDown")
                .With("Up", "<Keyboard>/e")
                .With("Down", "<Keyboard>/q")
                .With("Up", "<Gamepad>/rightshoulder")
                .With("Down", "<Gamepad>/leftshoulder");
            boostFactorAction.AddBinding("<Gamepad>/Dpad").WithProcessor("scaleVector2(x=1, y=4)");

            movementAction.Enable();
            lookAction.Enable();
            verticalMovementAction.Enable();
            boostFactorAction.Enable();
        }
#endif

        void OnEnable()
        {
            m_TargetCameraState.SetFromTransform(transform);
            m_InterpolatingCameraState.SetFromTransform(transform);
        }

        Vector3 GetInputTranslationDirection()
        {
            Vector3 direction = Vector3.zero;
#if ENABLE_INPUT_SYSTEM
            var moveDelta = movementAction.ReadValue<Vector2>();
            direction.x = moveDelta.x;
            direction.z = moveDelta.y;
            direction.y = verticalMovementAction.ReadValue<Vector2>().y;
#else
            if (Input.GetKey(KeyCode.W))
            {
                direction += Vector3.forward;
            }
            if (Input.GetKey(KeyCode.S))
            {
                direction += Vector3.back;
            }
            if (Input.GetKey(KeyCode.A))
            {
                direction += Vector3.left;
            }
            if (Input.GetKey(KeyCode.D))
            {
                direction += Vector3.right;
            }
            if (Input.GetKey(KeyCode.Q))
            {
                direction += Vector3.down;
            }
            if (Input.GetKey(KeyCode.E))
            {
                direction += Vector3.up;
            }
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                m_TargetCameraState.yaw -= boostRotation;
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                m_TargetCameraState.yaw += boostRotation;
            }
            if (Input.GetKey(KeyCode.UpArrow))
            {
                m_TargetCameraState.pitch -= boostRotation;
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                m_TargetCameraState.pitch += boostRotation;
            }
            if (Input.GetKey(KeyCode.X) || Input.GetKey(KeyCode.LeftCommand))
            {
                SwitchList.Switch();
            }
            if (Input.GetKey(KeyCode.V))
            {
                SwitchList.VizToggle();
            }
            if (Input.GetKey(KeyCode.Space))
            {
                InteractScript.Toggle();
                if (InteractScript._hasGrappleHit)
                {
                    Vector3 point = InteractScript.GetGrapplePoint();
                    m_TargetCameraState.RawTranslate(new Vector3(
                        (point.x - m_TargetCameraState.x) * 1f,
                        (point.y - m_TargetCameraState.y) * 1f + 1.0f,
                        (point.z - m_TargetCameraState.z) * 1f));
                    InteractScript.ClearGrappleHit();
                }

            }
#endif
            return direction;
        }

        void FixedUpdate()
        {
            // Speed up movement with specific tool
            if (InteractScript.GetEffector().Type == "teleport")
            {
                boost = 4f;
            }
            else
            {
                boost = 1.5f;
            }

            // Change colour of fog and density if under water
            if (m_TargetCameraState.y >= 0f)
            {
                RenderSettings.fogDensity = 0.01f;
                RenderSettings.fogColor = new Color(0.5f, 0.78f, 1.0f);
                if (IsUnderwater)
                {
                    GetComponentInParent<AudioSource>().clip = AboveWaterAudio;
                    GetComponentInParent<AudioSource>().Play();
                }
                IsUnderwater = false;

            }
            else
            {
                RenderSettings.fogDensity = 0.1f;
                RenderSettings.fogColor = new Color(0.4f + m_TargetCameraState.y / 30f, 0.78f + m_TargetCameraState.y / 30f, 0.9f + m_TargetCameraState.y /30f);
                if (!IsUnderwater)
                {
                    GetComponentInParent<AudioSource>().clip = BelowWaterAudio;
                    GetComponentInParent<AudioSource>().Play();
                }
                IsUnderwater = true;
            }


            // Exit Sample 
            if (IsEscapePressed())
            {
                Application.Quit();
				#if UNITY_EDITOR
				UnityEditor.EditorApplication.isPlaying = false; 
				#endif
            }

            // Hide and lock cursor when right mouse button pressed
            if (IsRightMouseButtonDown())
            {
                Cursor.lockState = CursorLockMode.Locked;
            }

            // Unlock and show cursor when right mouse button released
            if (IsRightMouseButtonUp())
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            // Rotation
            if (IsCameraRotationAllowed())
            {
                var mouseMovement = GetInputLookRotation() * k_MouseSensitivityMultiplier * mouseSensitivity;
                if (invertY)
                    mouseMovement.y = -mouseMovement.y;
                
                var mouseSensitivityFactor = mouseSensitivityCurve.Evaluate(mouseMovement.magnitude);

                m_TargetCameraState.yaw += mouseMovement.x * mouseSensitivityFactor;
                m_TargetCameraState.pitch += mouseMovement.y * mouseSensitivityFactor;
            }
            
            // Translation
            var translation = GetInputTranslationDirection() * Time.deltaTime;

            // Speed up movement when shift key held
            if (IsBoostPressed())
            {
                translation *= 10.0f;
            }
            
            // Modify movement by a boost factor (defined in Inspector and modified in play mode through the mouse scroll wheel)
            boost += GetBoostFactor();
            translation *= Mathf.Pow(2.0f, boost);

            // If in contact with terrain, push away to signal collision and prevent contact motion
            RaycastHit hit;
            bool caveHit = false;

            // Check for collisions in the XZ plane (and directly below/above)
            Vector3[] transforms = {
                new Vector3(0, 1, 0), // UP
                new Vector3(0, -1, 0), // DOWN
                new Vector3(1, 1, 0),
                new Vector3(1, -1, 0),
                new Vector3(-1, 1, 0),
                new Vector3(-1, -1, 0),
                new Vector3(0, 1, 1),
                new Vector3(0, -1, 1),
                new Vector3(0, 1, -1),
                new Vector3(0, -1, -1),
                new Vector3(1, 0, 0),
                new Vector3(-1, 0, 0),
                new Vector3(0, 0, 1),
                new Vector3(0, 0, -1),
                new Vector3(1, 0, 1),
                new Vector3(-1, 0, -1),
                new Vector3(1, 0, -1),
                new Vector3(-1, 0, 1),
                new Vector3(1, 1, 1),
                new Vector3(-1, 1, -1),
                new Vector3(1, 1, -1),
                new Vector3(-1, 1, 1),
                new Vector3(1, -1, 1),
                new Vector3(-1, -1, -1),
                new Vector3(1, -1, -1),
                new Vector3(-1, -1, 1)
            };
            foreach (Vector3 t in transforms)
            {
                Vector3 tr = t;
                if (Physics.Raycast(transform.position, tr, out hit, TerrainCollisionThreshold))
                {
                    if (hit.collider.tag == "Terrain" || hit.collider.tag == "Cave" || hit.collider.tag == "SolidSetPiece" || hit.collider.tag == "SolidObject")
                    {
                        m_TargetCameraState.RawTranslate(-tr * 0.01f * Mathf.Pow(2.0f, boost));
                    }
                }

                // Check if close to or inside a cave
                if (Physics.Raycast(transform.position, tr, out hit, CaveCollisionThreshold))
                    if (hit.collider.tag == "Cave")
                    {
                        InCave = true;
                        caveHit = true;
                    }

            }
            if (!caveHit)
                InCave = false;

            m_TargetCameraState.Translate(translation);

            // Check to see if we are outside play area bounds
            m_TargetCameraState.x = Mathf.Clamp(m_TargetCameraState.x, -PlayPerimiter, PlayPerimiter);
            m_TargetCameraState.y = Mathf.Clamp(m_TargetCameraState.y, -PlayPerimiter / 2.0f, PlayPerimiter / 2.0f);
            m_TargetCameraState.z = Mathf.Clamp(m_TargetCameraState.z, -PlayPerimiter, PlayPerimiter);

            // Framerate-independent interpolation
            // Calculate the lerp amount, such that we get 99% of the way to our target in the specified time
            var positionLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / positionLerpTime) * Time.deltaTime);
            var rotationLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / rotationLerpTime) * Time.deltaTime);
            m_InterpolatingCameraState.LerpTowards(m_TargetCameraState, positionLerpPct, rotationLerpPct);

            m_InterpolatingCameraState.UpdateTransform(transform);
        }

        void LateUpdate()
        {
            // Check if character is below any playable terrains
            // And correct the target camera state if so
            foreach (Terrain t in Terrain.activeTerrains)
            {
                if (t.gameObject.tag == "Terrain")
                {
                    Vector3 cPos = transform.position;
                    Bounds bounds = new Bounds(t.terrainData.bounds.center
                        + t.transform.position,
                        t.terrainData.bounds.size);
                    if (bounds.Contains(cPos))
                    {
                        float posY = m_TargetCameraState.y;
                        float tPosY = t.SampleHeight(cPos) + t.GetPosition().y;
                        if (!InCave) // Disable check if under terrain
                        {
                            m_TargetCameraState.y = Mathf.Max(posY, tPosY + 0.5f);
                            transform.position = new Vector3(transform.position.x, Mathf.Max(transform.position.y, tPosY + 0.5f), transform.position.z);
                        }
                    }
                }
            }

            // Control lighting with respect to depth below sea level
            Sun.GetComponent<Light>().intensity = Mathf.Clamp(1 + transform.position.y / 20f, 0f, 1f);
            PlayerLight.GetComponent<Light>().intensity = Mathf.Clamp(-transform.position.y, 1f, 30f);
        }

        float GetBoostFactor()
        {
#if ENABLE_INPUT_SYSTEM
            return boostFactorAction.ReadValue<Vector2>().y * 0.01f;
#else
            return Input.mouseScrollDelta.y * 0.01f;
#endif
        }

        Vector2 GetInputLookRotation()
        {
            // try to compensate the diff between the two input systems by multiplying with empirical values
#if ENABLE_INPUT_SYSTEM
            var delta = lookAction.ReadValue<Vector2>();
            delta *= 0.5f; // Account for scaling applied directly in Windows code by old input system.
            delta *= 0.1f; // Account for sensitivity setting on old Mouse X and Y axes.
            return delta;
#else
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
        }

        bool IsBoostPressed()
        {
#if ENABLE_INPUT_SYSTEM
            bool boost = Keyboard.current != null ? Keyboard.current.leftShiftKey.isPressed : false; 
            boost |= Gamepad.current != null ? Gamepad.current.xButton.isPressed : false;
            return boost;
#else
            return Input.GetKey(KeyCode.LeftShift);
#endif

        }

        bool IsEscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null ? Keyboard.current.escapeKey.isPressed : false; 
#else
            return Input.GetKey(KeyCode.Escape);
#endif
        }

        bool IsCameraRotationAllowed()
        {
#if ENABLE_INPUT_SYSTEM
            bool canRotate = Mouse.current != null ? Mouse.current.rightButton.isPressed : false;
            canRotate |= Gamepad.current != null ? Gamepad.current.rightStick.ReadValue().magnitude > 0 : false;
            return canRotate;
#else
            return Input.GetMouseButton(1);
#endif
        }

        bool IsRightMouseButtonDown()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.rightButton.isPressed : false;
#else
            return Input.GetMouseButtonDown(1);
#endif
        }

        bool IsRightMouseButtonUp()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? !Mouse.current.rightButton.isPressed : false;
#else
            return Input.GetMouseButtonUp(1);
#endif
        }

    }

}
