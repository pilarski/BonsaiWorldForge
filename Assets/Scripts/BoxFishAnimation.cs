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

// This file is responsible for animating fish animals and shoals.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxFishAnimation : MonoBehaviour
{

    public Vector3 CentrePointOfMotion;
    public Vector3 ExtentOfMotion;
    public Vector3 CurrentMotionTarget;
    public Vector3 PreviousMotionTarget;

    public float WaveMotionDisplacementAmmount;
    public float BaseSpeed;
    [Tooltip("Fish will not exceed this height.")]
    public float ShallowestDepthLimit;

    public GameObject ShoalLeader;
    public bool IsLeader;

    private Animator _animator;
    private float _timeSinceTargetChange;
    private int _speedOfSwimTranslation;
    private float _switchTime = 5f;
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
        material.color = new Color(Mathf.Clamp(Random.value, 0.7f, 1.0f),
            Mathf.Clamp(Random.value, 0.7f, 1.0f),
            Mathf.Clamp(Random.value, 0.7f, 1.0f));
        _rigidbody = GetComponent<Rigidbody>();
        _rotationFreeze = true;
    }

    private Vector3 GetNewMotionTarget()
    {
        Vector3 newTarget = new Vector3();
        if (IsLeader || ShoalLeader == null)
        {
            newTarget = Random.insideUnitSphere;
            newTarget.x = CentrePointOfMotion.x + newTarget.x * ExtentOfMotion.x;
            newTarget.y = CentrePointOfMotion.y + newTarget.y * ExtentOfMotion.y;
            newTarget.z = CentrePointOfMotion.z + newTarget.z * ExtentOfMotion.z;
        }
        else
        {
            newTarget = ShoalLeader.GetComponent<BoxFishAnimation>().CurrentMotionTarget;
            newTarget.x += -1f + 2f * Random.value;
            newTarget.y += -0.5f + 1f * Random.value;
            newTarget.z += -1f + 2f * Random.value;
        }
        _speedOfSwimTranslation = Random.Range(0, 3);
        _animator.SetInteger("SwimSpeed", _speedOfSwimTranslation);
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
        // Only let fish roate in Y-axis, as fish are prone to do
        FreezeXZRotations();

        // Smoothly interpolate between targets 
        var target = Vector3.Lerp(PreviousMotionTarget, CurrentMotionTarget, Mathf.Clamp(_timeSinceTargetChange / 2f, 0f, 1f));

        // Tanslate according to swim speed
        //float speed = (float)_animator.GetInteger("SwimSpeed") * BaseSpeed * Time.deltaTime;
        float speed = (float)_speedOfSwimTranslation * BaseSpeed * Time.deltaTime;
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
        transform.Translate(transform.forward * WaveMotionDisplacementAmmount * Mathf.Sin(Time.time));

        // Check to see if fish is swimming in air
        if (transform.position.y > ShallowestDepthLimit)
        {
            Vector3 pos = new Vector3(transform.position.x, ShallowestDepthLimit, transform.position.z);
            Quaternion rot = transform.rotation;
            transform.SetPositionAndRotation(pos, rot);
        }

    }
}
