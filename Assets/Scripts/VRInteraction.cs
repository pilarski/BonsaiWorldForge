/*
   Copyright 2022 Patrick M. Pilarski
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

// This file is responsible for player control in virtual reality (VR)
// and for managing collision detection with the environment and changing
// lighting/fog in response to environmental location.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRInteraction : MonoBehaviour
{
    public SwitchList SwitchList;
    public Interact InteractScript;

    private UnityEngine.XR.InputDevice _armDevice;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //if (_armDevice == null)
        //{
            var rightHandDevices = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesAtXRNode(UnityEngine.XR.XRNode.RightHand, rightHandDevices);
            UnityEngine.XR.InputDevice _armDevice = rightHandDevices[0]; // For now, pick first detected, assuming only one in this project
            //TODO: add ability to select right or left hand assignment here, based on tick box on component or config file
        //}

        bool buttonValue;
        if (_armDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out buttonValue) && buttonValue)
        {
            //Debug.Log("Trigger button is pressed."); // This is indeed the trigger
            InteractScript.Toggle();
            /*
            * For now, no checking for grapple beam; TODO: fix once camera translation is sorted.
            if (InteractScript._hasGrappleHit)
            {
                Vector3 point = InteractScript.GetGrapplePoint();
                m_TargetCameraState.RawTranslate(new Vector3(
                    (point.x - m_TargetCameraState.x) * 1f,
                    (point.y - m_TargetCameraState.y) * 1f + 1.0f,
                    (point.z - m_TargetCameraState.z) * 1f));
                InteractScript.ClearGrappleHit();
            }*/
        }
        if (_armDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out buttonValue) && buttonValue)
        {
            //Debug.Log("Primary button is pressed."); // This is the "A" button for Vive Focus 3
            SwitchList.Switch();
        }
    }
}
