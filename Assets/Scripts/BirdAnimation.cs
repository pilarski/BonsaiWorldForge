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

// This file is responsible for animating bird animals and flocks.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BirdAnimation : MonoBehaviour
{

    public Vector3 CentrePointOfMotion;
    public Vector3 ExtentOfMotion;
    public Vector3 CurrentMotionTarget;
    public Vector3 PreviousMotionTarget;

    public float WindMotionDisplacementAmmount;
    public float BaseSpeed;
    [Tooltip("Bird will not go lower than this height when diving.")]
    public float DiveDepthLimit;

    public GameObject FlockLeader;
    public bool IsLeader;

    private Animator _animator;
    private float _timeSinceTargetChange;
    private float _speedOfFlightTranslation;
    private float _switchTime = 5f;
    private float _windModifier = 1f;
    private string _clipName;
    private Rigidbody _rigidbody;
    private bool _rotationFreeze;

    // Start is called before the first frame update
    void Start()
    {
        _animator = GetComponent<Animator>();
        CentrePointOfMotion = transform.position;
        PreviousMotionTarget = transform.position;
        CurrentMotionTarget = GetNewMotionTarget();
        Material material = gameObject.GetComponentInChildren<Renderer>().material;
        material.color = new Color(Mathf.Clamp(Random.value, 0.85f, 1.0f),
            Mathf.Clamp(Random.value, 0.85f, 1.0f),
            Mathf.Clamp(Random.value, 0.85f, 1.0f));
        _rigidbody = GetComponent<Rigidbody>();
        _rotationFreeze = true;
    }

    private Vector3 GetNewMotionTarget()
    {
        // Only update targets when not diving
        if (_clipName == "Dive" ||
            _clipName == "Rising")
            return CurrentMotionTarget;

        // Check for transitions to other animations when hovering
        if (_clipName == "Hover")
        {
            float rand = Random.value;
            if (rand < 0.15f)
                _animator.SetBool("IsDiving", true);
            else if (rand < 0.4f)
                _animator.SetBool("IsGliding", true);
            else if (rand < 0.75f)
                _animator.SetBool("IsFlapping", true);
        }

        Vector3 newTarget = new Vector3();
        if (IsLeader || FlockLeader == null)
        {
            newTarget = Random.insideUnitSphere;
            newTarget.x = CentrePointOfMotion.x + newTarget.x * ExtentOfMotion.x;
            newTarget.y = CentrePointOfMotion.y + newTarget.y * ExtentOfMotion.y;
            newTarget.z = CentrePointOfMotion.z + newTarget.z * ExtentOfMotion.z;
        }
        else
        {
            newTarget = FlockLeader.GetComponent<BirdAnimation>().CurrentMotionTarget;
            newTarget.x += -1f + 2f * Random.value;
            newTarget.y += -0.5f + 1f * Random.value;
            newTarget.z += -1f + 2f * Random.value;
        }

        return newTarget;
    }

    // Freeze rotations selectively based on these functions
    private void AllowRotations()
    {
        _rigidbody.constraints = RigidbodyConstraints.None;
        _rotationFreeze = false;

    }

    private void FreezeXZRotations()
    {
        _rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        _rotationFreeze = true;
    }


    // Update movement based on animation state
    private void CheckAnimationState()
    {
        AnimatorClipInfo[] currentClipInfo = _animator.GetCurrentAnimatorClipInfo(0);
        _clipName = currentClipInfo[0].clip.name;

        if (_clipName == "Hover")
        {
            FreezeXZRotations();
            _animator.SetBool("IsRising", false);
            _animator.SetBool("IsDiving", false);
            _animator.SetBool("IsFlapping", false);
            _animator.SetBool("IsGliding", false);
            _animator.SetBool("DiveDepthReached", false);
            _speedOfFlightTranslation = 0f;
            _windModifier = 1f;
        }
        else if (_clipName == "Flap")
        {
            FreezeXZRotations();
            _speedOfFlightTranslation = 1f;
            _windModifier = 0.8f;
            
        }
        else if (_clipName == "Rising")
        {
            FreezeXZRotations();
            _animator.SetBool("IsRising", true);
            _animator.SetBool("IsDiving", false);
            _animator.SetBool("DiveDepthReached", false);
            _speedOfFlightTranslation = 6f;
            _windModifier = 0.3f;
            CurrentMotionTarget.y = CentrePointOfMotion.y + 5f;
        }
        else if (_clipName == "Glide")
        {
            FreezeXZRotations();
            _speedOfFlightTranslation = 3f;
            _windModifier = 0.5f;
        }
        else if (_clipName == "Dive")
        {
            AllowRotations();
            _windModifier = 0.05f;
            _animator.SetBool("IsDiving", true);
            _speedOfFlightTranslation = 20f;
            CurrentMotionTarget.y = DiveDepthLimit - 5f;
            _animator.SetBool("CruisingAltitude", false);
        }
        else
        {
            AllowRotations();
            _windModifier = 1f;
            _speedOfFlightTranslation = Random.Range(0f, 2f);
        }

        _animator.SetFloat("FlightSpeed", _speedOfFlightTranslation);
    }

    // Update is called once per frame
    void Update()
    {
        // Check for target change
        _timeSinceTargetChange += Time.deltaTime;
        if (_timeSinceTargetChange > _switchTime)
        {
            _timeSinceTargetChange = 0f;
            PreviousMotionTarget = CurrentMotionTarget;
            CurrentMotionTarget = GetNewMotionTarget();
            _switchTime = 4f + 2f * Random.value;
        }
    }

    void FixedUpdate()
    {

        // Update speed and targets of motion based on animations
        CheckAnimationState();

        // Smoothly interpolate between targets 
            var target = Vector3.Lerp(PreviousMotionTarget, CurrentMotionTarget, Mathf.Clamp(_timeSinceTargetChange / 0.5f, 0f, 1f));

        // Tanslate according to swim speed
        float speed = (float)_speedOfFlightTranslation * BaseSpeed * Time.deltaTime;
        // Check for look rotation
        Vector3 newPos = target - transform.position;
        // Pin axis rotation if frozen
        if (_rotationFreeze)
        {
            newPos.y = 0f;
        }
        var lookRotation = Vector3.RotateTowards(transform.forward, newPos, 1f * Time.deltaTime, 0.0f);
        transform.rotation = Quaternion.LookRotation(lookRotation);
        transform.position = Vector3.MoveTowards(transform.position, target, speed);

        // Simulate gentle back and forth due to wave motion
        transform.Translate(transform.right * WindMotionDisplacementAmmount * _windModifier * Mathf.Sin(Time.time));

        // Check if bird is at flight height
        if (transform.position.y > CentrePointOfMotion.y)
            _animator.SetBool("CruisingAltitude", true);

        // Check to see if bird is below dive limit
        if (transform.position.y < DiveDepthLimit)
        {
            _animator.SetBool("DiveDepthReached", true);
            Vector3 pos = new Vector3(transform.position.x, DiveDepthLimit, transform.position.z);
            Quaternion rot = transform.rotation;
            transform.SetPositionAndRotation(pos, rot);
        }

    }
}
