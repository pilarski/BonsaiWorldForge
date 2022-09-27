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

// This file is responsible for interaction between the arm effectors
// and the environment, acutating grippable objects, and arm material changes.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interact : MonoBehaviour
{
    public SwitchList ThisSwitchList;
    public TerraformControl TerraformControlInstance;
    public GameObject EffectVolumeVisualObject;
    public GameObject PlacerVolumeVisualObject;
    public GameObject TeleportLineVisualObject;
    public AudioClip HammerStrikeAudio;
    public AudioClip HammerStrikeFinalAudio;
    public AudioClip AttachAudio;
    public AudioClip ToggleAudio;
    public Material ArmShellMaterial;
    public bool ToggleState = false;
    public bool HeldObject = false;
    public Collider ContactObj;
    public Collider CurrentHeldObj;
    public int ContactObjSize;
    public int EffectorSize;

    [ColorUsage(true, true)]
    public Color ShellDefaultColor;// = new Vector4(0.4f, 0.7f, 1.0f);
    [ColorUsage(true, true)]
    public Color ShellDefaultColorHologram;// = new Vector4(1.0f, 32.2f, 32.23f);
    [ColorUsage(true, true)]
    public Color ShellColorPositive; // = Color.green;
    [ColorUsage(true, true)]
    public Color ShellColorPositiveHologram; // = Color.green;
    [ColorUsage(true, true)]
    public Color ShellColorNegative; // = Color.red;
    [ColorUsage(true, true)]
    public Color ShellColorNegativeHologram; // = Color.red;
    [ColorUsage(true, true)]
    public Color ShellColorActive;// = Color.yellow;
    [ColorUsage(true, true)]
    public Color ShellColorActiveHologram;// = Color.yellow;

    private float _toggleTimeRemaining = 0f;
    private MeshRenderer _volumeRenderer;
    private MeshRenderer _volumePlacerRenderer;
    private LineRenderer _lineTeleportRenderer;
    //private Material _lineTeleportMaterial;
    private Effector _effector;
    private int _lastIndex;
    private RaycastHit _hit;
    public bool _hasGrappleHit;
    private float _hitRange = 200f;
    private float _hitRangeMin = 0.5f;

    private AudioSource _audioSource;

    // Start is called before the first frame update
    void Start()
    {
        _volumeRenderer = EffectVolumeVisualObject.transform.GetComponent<MeshRenderer>();
        _volumeRenderer.enabled = false;
        _volumePlacerRenderer = PlacerVolumeVisualObject.transform.GetComponent<MeshRenderer>();
        _volumePlacerRenderer.enabled = false;
        _lineTeleportRenderer = TeleportLineVisualObject.transform.GetComponent<LineRenderer>();
        _lineTeleportRenderer.enabled = false;
        _lineTeleportRenderer.SetPosition(1, transform.forward + new Vector3(0f, 0f, _hitRange));
        //_lineTeleportMaterial = TeleportLineVisualObject.GetComponent<Renderer>().material;
        ColourShellDefault();
        _audioSource = GetComponent<AudioSource>();
        GameObject e = ThisSwitchList.Effectors[0];
        _effector = e.GetComponent<Effector>();

    }

    // Update is called once per frame
    void Update()
    {
        if (_toggleTimeRemaining > 0)
            _toggleTimeRemaining -= Time.deltaTime;
        int index = ThisSwitchList.ActiveIndex;
        // Drop any held item on switching of effectors
        if (index != _lastIndex)
            DropHeldObject();
        GameObject e = ThisSwitchList.Effectors[index];
        _effector = e.GetComponent<Effector>();
        EffectorSize = _effector.Size;
        _lastIndex = index;
    }


    private void FixedUpdate()
    {
        if (_effector.Type == "grapple" && _toggleTimeRemaining <= 0f && _effector.Attached == true)
        {
            _lineTeleportRenderer.enabled = true;
            _hasGrappleHit = Physics.Raycast(transform.position, transform.forward, out _hit, _hitRange);
            if (_hasGrappleHit)
            {
                if (Vector3.Distance(transform.position, _hit.point) < _hitRangeMin)
                {
                    _hasGrappleHit = false;
                    _volumeRenderer.enabled = false;
                    _lineTeleportRenderer.enabled = false;
                    ColourShellNegative();
                }
                else
                {
                    _volumeRenderer.enabled = true;
                    _volumeRenderer.transform.position = _hit.point;
                    ColourShellPositive();
                }
            }
            else
            {
                _volumeRenderer.enabled = false;
                ColourShellDefault();
            }
        }
        else
        {
            ClearGrappleHit();
        }
    }

    public Vector3 GetGrapplePoint()
    {
        if (_hasGrappleHit)
        {
            return _hit.point;
        }
        else
        {
            return new Vector3(0f, 0f, 0f);
        }
    }

    public void ClearGrappleHit()
    {
        ColourShellDefault();
        _lineTeleportRenderer.enabled = false;
        _volumeRenderer.enabled = false;
        _hasGrappleHit = false;
    }


    private void ColourShellDefault()
    {
        ArmShellMaterial.SetColor("_color_emission", ShellDefaultColorHologram);
        ArmShellMaterial.SetColor("_color_emission_hologram", ShellDefaultColor);
        //_lineTeleportMaterial.SetColor("_color", ShellDefaultColorHologram);
    }

    private void ColourShellPositive()
    {
        ArmShellMaterial.SetColor("_color_emission", ShellColorPositiveHologram); //_EmissionColor
        ArmShellMaterial.SetColor("_color_emission_hologram", ShellColorPositive * 1f);
        //_lineTeleportMaterial.SetColor("_color", ShellColorPositiveHologram);
    }

    private void ColourShellNegative()
    {
        ArmShellMaterial.SetColor("_color_emission", ShellColorNegativeHologram);
        ArmShellMaterial.SetColor("_color_emission_hologram", ShellColorNegative * 1f);
        //_lineTeleportMaterial.SetColor("_color", ShellColorNegativeHologram);
    }

    private void ColourShellActive()
    {
        ArmShellMaterial.SetColor("_color_emission", ShellColorActiveHologram); //_EmissionColor
        ArmShellMaterial.SetColor("_color_emission_hologram", ShellColorActive * 1f);
    }

    void OnTriggerStay(Collider other)
    {
        Rigidbody attachedBody = other.attachedRigidbody;
        if (attachedBody && attachedBody.tag == "GrippableObject")
        {
            Grippable grippable = attachedBody.GetComponent<Grippable>();
            if (grippable.Size == EffectorSize
                && _effector.Type == "tongs"
                && grippable.IsFinalForm == false)
            {
                attachedBody.AddForce(Vector3.up * 10);
                ColourShellPositive();
            }
            else
            {
                ColourShellNegative();
            }

            if (grippable.OnAnvil
                && _effector.Type == "hammer"
                && grippable.Heat >= 5.0f
                && grippable.IsFinalForm == false)
            {
                ColourShellPositive();
            }

            if (grippable.IsFinalForm
                && _effector.Type == "placer"
                && !grippable.IsTool
                && grippable.IsPlaced == false)
            {
                ColourShellPositive();
            }

            if (HeldObject)
            {
                ColourShellActive();
            }

            if (grippable.IsTool && grippable.ToolName == _effector.Type &&
                grippable.ToolSize == _effector.Size && !_effector.Attached)
            {
                ColourShellPositive();
            }


            if (ContactObj == null && HeldObject == false && _effector.Type != "grapple")
            {
                ColourShellDefault();
            }



        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check for interaction with exit field
        if (string.Compare(other.gameObject.tag, "TransportField") == 0)
        {
            TerraformControlInstance.PlayerInTransportField = true;
        }

        if (ContactObj == null)
        {
            if (other.attachedRigidbody.tag == "GrippableObject" && other.attachedRigidbody.GetComponent<Grippable>() != null)
            {
                Grippable g = other.attachedRigidbody.GetComponent<Grippable>();
                ContactObjSize = g.Size;
                ContactObj = other;
                if (g.OnAnvil && _effector.Type == "hammer" && g.Heat >= 5.0f && g.IsFinalForm == false && _effector.Attached == true)
                {
                    g.Hits[_effector.Size - 1] += 1;
                    g.Sparks.Emit(50);
                    if (g.Hits[0] + g.Hits[1] + g.Hits[2] == 1)
                    {
                        _audioSource.PlayOneShot(HammerStrikeAudio,0.4f);
                        other.GetComponent<MeshRenderer>().enabled = false;
                        other.GetComponent<Grippable>().Deform1Enabled = true;
                        //other.transform.rotation = Quaternion.identity;
                    }
                    else if (g.Hits[0] + g.Hits[1] + g.Hits[2] == 2)
                    {
                        _audioSource.PlayOneShot(HammerStrikeAudio, 0.5f);
                        other.GetComponent<Grippable>().Deform1Enabled = false;
                        other.GetComponent<Grippable>().Deform2Enabled = true;
                        //other.transform.rotation = Quaternion.identity;
                    }

                    if (g.Hits[0] + g.Hits[1] + g.Hits[2] >= 3)
                    {
                        _audioSource.PlayOneShot(HammerStrikeFinalAudio, 0.7f);
                        g.MaxHitIndex = 0;
                        int maxhits = 0;
                        for (int i = 0; i < 3; i++)
                        {
                            if (g.Hits[i] > maxhits)
                            {
                                g.MaxHitIndex = i + 1;
                                maxhits = g.Hits[i];
                            }
                        }
                        if (g.Hits[g.MaxHitIndex - 1] == 3)
                            // Hammers blows of all the same weight make rocks
                            g.IsOrganic = false;
                        else
                        {
                            // Complex hammer blows of different weights make plants
                            g.IsOrganic = true;
                        }
                        g.IsFinalForm = true;
                        g.TerrainAssetIndex = g.Size - 1;
                        other.GetComponent<Rigidbody>().freezeRotation = true;
                        other.transform.rotation = Quaternion.identity;
                        other.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
                        GameObject childObject = null;
                        if (g.IsOrganic)
                        {
                            childObject = Instantiate(g.FinalForm) as GameObject;
                        }
                        else
                        {
                            childObject = Instantiate(g.FinalFormInorganic) as GameObject;
                        }
                        childObject.name = "FinalForm";
                        childObject.transform.parent = other.transform;
                        childObject.transform.localPosition = new Vector3(0, 0, 0);
                        childObject.transform.localRotation = Quaternion.identity;
                        childObject.GetComponent<Rigidbody>().isKinematic = true;
                        childObject.GetComponent<Rigidbody>().detectCollisions = false;
                        // fix the ratio for the palm tree
                        float correction = 1.0f;
                        if (g.IsOrganic)
                            correction = 100.0f;  // Scale organics based on their specific mesh sizes
                        if (!g.IsOrganic)
                        {
                            correction = 2.5f; // Scale inorganics based on their specific mesh sizes
                            g.TerrainAssetIndex += 3; // move to the "boulder region" of the assets in the terrain indicies
                        }
                        if (!g.IsTool)
                            childObject.transform.localScale = new Vector3(0.05f * (float)g.MaxHitIndex * correction, 0.05f * (float)g.MaxHitIndex * correction, 0.05f * (float)g.MaxHitIndex * correction);
                        else
                        {
                            childObject.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                        }
                        //childObject.GetComponent<Rigidbody>().freezeRotation = true;
                        other.GetComponent<MeshRenderer>().enabled = false;
                        other.GetComponent<Grippable>().Deform1Enabled = false;
                        other.GetComponent<Grippable>().Deform2Enabled = false;


                        // Check if tool object, and if so, unlock tool
                        if (g.IsTool)
                        {
                            string name = g.ToolName;
                            foreach (GameObject eg in ThisSwitchList.Effectors)
                            {
                                if (eg.GetComponent<Effector>().Type == name && eg.GetComponent<Effector>().Unlocked == false)
                                {
                                    if (g.ToolSize == eg.GetComponent<Effector>().Size)
                                        eg.GetComponent<Effector>().Unlocked = true;
                                }
                            }
                        }

                    }
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        ContactObj = null;
        ContactObjSize = 0;
        if (HeldObject == false)
        {
            ColourShellDefault();
        }

        // Check for interaction with exit field
        if (other.gameObject.tag == "TransportField")
            TerraformControlInstance.PlayerInTransportField = false;

    }

    public void Toggle()
    {
        if (_toggleTimeRemaining > 0f)
            return;

        _toggleTimeRemaining = 0.2f;

        Grippable grippable = null;
        if (ContactObj)
            grippable = ContactObj.attachedRigidbody.GetComponent<Grippable>();

        if (!(HeldObject == true && CurrentHeldObj.attachedRigidbody.GetComponent<Grippable>().IsPlaced && _effector.Type == "placer"))
            _audioSource.PlayOneShot(ToggleAudio, 0.1f);

        if (ContactObj != null && grippable.IsTool
            && grippable.ToolName == _effector.Type
            && grippable.ToolSize == _effector.Size
            && !_effector.Attached)
        {
            _audioSource.PlayOneShot(AttachAudio, 0.1f);
            _effector.Attached = true;
            ThisSwitchList.UpdateVisibility();
            Destroy(ContactObj.gameObject);
            return;
        }

        if (ContactObj != null && HeldObject == false && _effector.Type == "tongs"
            && grippable.Size == EffectorSize
            && grippable.IsFinalForm == false
            && _effector.Attached == true)
        {
            CurrentHeldObj = ContactObj;
            CurrentHeldObj.attachedRigidbody.transform.parent = gameObject.transform;
            CurrentHeldObj.attachedRigidbody.transform.localPosition = new Vector3(0, 0, 0);
            CurrentHeldObj.attachedRigidbody.isKinematic = true;
            HeldObject = true;

        }
        else if (ContactObj != null && HeldObject == false && _effector.Type == "placer"
            && grippable.IsFinalForm
            && _effector.Attached == true)
        {
            CurrentHeldObj = ContactObj;
            CurrentHeldObj.attachedRigidbody.transform.parent = gameObject.transform;
            CurrentHeldObj.transform.localPosition = new Vector3(0,0,8f);
            CurrentHeldObj.attachedRigidbody.isKinematic = true;
            HeldObject = true;
        }
        else if (HeldObject == true)
        {
            CurrentHeldObj.attachedRigidbody.transform.parent = null;
            CurrentHeldObj.attachedRigidbody.isKinematic = false;
            CurrentHeldObj = null;
            HeldObject = false;
        }

        //if (_effector.Type == "teleport")
        //{
          
        //}
    }

    public void DropHeldObject()
    {
        if (!CurrentHeldObj)
            return;
        CurrentHeldObj.attachedRigidbody.transform.parent = null;
        CurrentHeldObj.attachedRigidbody.isKinematic = false;
        CurrentHeldObj = null;
        HeldObject = false;
    }
    
    public float GetToggleTimeRemaining()
    {
        return _toggleTimeRemaining;
    }

    public Effector GetEffector()
    {
        return _effector;
    }



}
