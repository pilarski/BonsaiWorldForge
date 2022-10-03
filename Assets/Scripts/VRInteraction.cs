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
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VRInteraction : MonoBehaviour
{
    public SwitchList SwitchList;
    public Interact InteractScript;
    public Volume _volume;

    private UnityEngine.XR.InputDevice _armDevice;
    private Effector _effector;

    private float _actionTimeRemaining;
    private float _actionTimeRemainingReset = 0.5f;

    private Vignette _vignette;

    // Start is called before the first frame update
    void Start()
    {
        //_volume = GetComponent<PostProcessVolume>();
        _volume.profile.TryGet<Vignette>(out _vignette);
        //_vignette.enabled.Override(true);
        //_vignette.intensity.Override(0f);

    }

    // Update is called once per frame
    void Update()
    {
        // Timer operations
        if (_actionTimeRemaining > 0f)
            _actionTimeRemaining -= Time.deltaTime;

        // Control the vignette tunnel for teleporting to be only shown for 100ms (temp: trying 50ms during editor mode; redo for built copy)
        if (_actionTimeRemainingReset - _actionTimeRemaining > 0.05f)
        {
            _vignette.intensity.value = 0.0f;
        }
        // Double check action timer does not drop below zero. TODO: change to floor/ceiling? What is faster?
        if (_actionTimeRemaining < 0f)
            _actionTimeRemaining = 0f;


        // Find out the current effector
        _effector = InteractScript.GetEffector();

        //TODO: add ability to select right or left hand assignment here, based on tick box on component or config file
        var rightHandDevices = new List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevicesAtXRNode(UnityEngine.XR.XRNode.RightHand, rightHandDevices);
        //if (rightHandDevices.Count > 0)
        //{
        UnityEngine.XR.InputDevice _armDevice = rightHandDevices[0]; // For now, pick first detected, assuming only one in this project
        //}

        bool buttonValue;
        if (_armDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out buttonValue) && buttonValue)
        {
            InteractScript.Toggle();

            if (_effector.Type == "teleport" && _actionTimeRemaining <= 0f)
            {
                teleportAlongArmDirection(10.0f);
                _actionTimeRemaining = _actionTimeRemainingReset;
            }

            if (InteractScript._hasGrappleHit)
            {
                Vector3 point = InteractScript.GetGrapplePoint();
                transform.parent.position += (new Vector3(
                    (point.x - transform.parent.position.x) * 1f,
                    (point.y - transform.parent.position.y) * 1f + 1.0f,
                    (point.z - transform.parent.position.z) * 1f));
                InteractScript.ClearGrappleHit();
                _vignette.intensity.value = 0.6f; // Blink aperture
            }
        }
        if (_armDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out buttonValue) && buttonValue)
        {
            // This is the "handle grip" button for Vive Focus 3
            SwitchList.Switch();
        }
        if (_armDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out buttonValue) && buttonValue)
        {
            // This is the "A" button for Vive Focus 3
            if (_actionTimeRemaining <= 0f)
            {
                teleportAlongArmDirection(-1.0f);
                _actionTimeRemaining = _actionTimeRemainingReset;
            }
        }
        if (_armDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out buttonValue) && buttonValue)
        {
            // This is the "B" button for Vive Focus 3
            if (_actionTimeRemaining <= 0f)
            {
                teleportAlongArmDirection(1.0f);
                _actionTimeRemaining = _actionTimeRemainingReset;
            }
        }

    }

    private void teleportAlongArmDirection(float distance)
    {
        //RaycastHit hit;
        //float thresholdDist = distance + 0.1f;
        //bool isTooClose = Physics.Raycast(transform.position, transform.rotation * Vector3.forward, out hit, distance);
        //if (Vector3.Distance(hit.point, transform.position) < thresholdDist)
        //    return;
        transform.parent.position += transform.rotation * (Vector3.forward * distance);
        _vignette.intensity.value = 0.6f; // Blink aperture
    }


}
