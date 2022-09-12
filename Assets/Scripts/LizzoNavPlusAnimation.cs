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

// This file is responsible for animating lizzard animals and lounges.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class LizzoNavPlusAnimation : MonoBehaviour
{
    public Vector3 CentrePointOfMotion;
    public Vector3 ExtentOfMotion;
    public Vector3 CurrentMotionTarget;
    public Vector3 PreviousMotionTarget;

    [Tooltip("Lizzo will start swimming at this depth when underwater.")]
    public float DivestartLimit;

    public GameObject LoungeLeader;
    public bool IsLeader;

    private NavMeshAgent _agent;
    private Animator _animator;
    private GameObject _childMeshArmature;

    private float _timeSinceTargetChange;
    private float _switchTime = 15f;


    // Start is called before the first frame update
    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.enabled = true;
        CleanNavMeshHit(transform.position, true); // Warp to mesh

        _animator = GetComponentInChildren<Animator>();

        CentrePointOfMotion = transform.position;
        PreviousMotionTarget = transform.position;
        CurrentMotionTarget = GetNewMotionTarget();
        Material material = gameObject.GetComponentInChildren<Renderer>().material;
        material.color = new Color(Mathf.Clamp(0.5f + 0.5f * Random.value, 0.5f, 1.0f),
                Mathf.Clamp(0.5f + 0.5f * Random.value, 0.5f, 1.0f),
                Mathf.Clamp(0.5f + 0.5f * Random.value, 0.5f, 1.0f));

        _childMeshArmature = transform.GetChild(0).gameObject;
    }

    

    private Vector3 GetNewMotionTarget()
    {
        Vector3 newTarget = new Vector3();
        if (IsLeader || LoungeLeader == null)
        {
            newTarget = Random.insideUnitSphere;
            newTarget.x = CentrePointOfMotion.x + newTarget.x * ExtentOfMotion.x;
            newTarget.y = CentrePointOfMotion.y + newTarget.y * ExtentOfMotion.y;
            newTarget.z = CentrePointOfMotion.z + newTarget.z * ExtentOfMotion.z;
        }
        else
        {
            newTarget = LoungeLeader.GetComponent<LizzoNavPlusAnimation>().CurrentMotionTarget;
            newTarget.x += -1f + 2f * Random.value;
            newTarget.y += -0.5f + 1f * Random.value;
            newTarget.z += -1f + 2f * Random.value;
        }
        //NavMeshHit hit;
        //NavMesh.SamplePosition(newTarget, out hit, 5f, NavMesh.AllAreas);
        //newTarget = hit.position;

        ////Backup code, in cases where agent spawns off mesh
        //if (!_agent.isOnNavMesh)
        //{
        //    _agent.Warp(hit.position);
        //    _agent.enabled = true;
        //    Debug.Log("Not on mesh, warping");
        //}
        newTarget = CleanNavMeshHit(newTarget, true);

        _agent.destination = newTarget;

        return newTarget;
    }

    // Sample navmesh target
    private Vector3 CleanNavMeshHit(Vector3 newTarget, bool warpIfOffMesh)
    {
        NavMeshHit hit;
        NavMesh.SamplePosition(newTarget, out hit, 5f, NavMesh.AllAreas);
        newTarget = hit.position;

        //Backup code, in cases where agent spawns off mesh
        if (!_agent.isOnNavMesh && warpIfOffMesh)
        {
            _agent.Warp(hit.position);
            _agent.enabled = true;
            Debug.Log("Not on mesh, warping");
        }

        return newTarget;
    }


// Align child object with mesh and armature ot terrain
private void AlignToTerrain(GameObject child)
    {
        RaycastHit hit;
        Vector3 modPos = transform.position;
        modPos[1] += 2f;
        if (Physics.Raycast(modPos,
            -transform.up,
            out hit, 10f))
        {
            if (hit.transform.tag == "Terrain") {
                Quaternion rotDesired = Quaternion.FromToRotation(transform.up, hit.normal)
                     * transform.rotation;
                rotDesired *= Quaternion.Euler(90, 0, 0); // adjust for blender mesh
                rotDesired = Quaternion.Lerp(child.transform.rotation, rotDesired, Time.deltaTime * 10f);
                child.transform.rotation = rotDesired; 
            }
        }
    }

    private void FixedUpdate()
    {
        AlignToTerrain(_childMeshArmature);
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
                _switchTime = 15f + 5f * Random.value;
            }

        // Check conditions and pass to animator
        if (transform.position.y < DivestartLimit)
            _animator.SetBool("isSwimming", true);
        else
            _animator.SetBool("isSwimming", false);
        float vel = Vector3.Magnitude(_agent.velocity);
        _animator.SetFloat("Velocity", vel);
        _animator.SetFloat("VelocityMultiplier", 1f + 2* vel);
    }
}
