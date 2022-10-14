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

// This file is responsible for maintaining and rendering
// the switching list of end effectors

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SwitchList : MonoBehaviour
{
    public GameObject[] Effectors;
    public int ActiveIndex;
    public int SwitchListLength;
    public AudioClip SwitchingSoundAudio;
    public AudioSource InteractionAudioSource;
    public bool AdaptiveSwitchingEnabled = false;

    //private bool _switchingListChanged = false;
    private float _switchTimeRemaining = 0f;
    private bool _visualizeList = false;
    private ToolPriorityManager _toolPriorityManager;
    //private GameObject[] _vizListObjects;

    // Start is called before the first frame update
    void Start()
    {
        Effectors = GameObject.FindGameObjectsWithTag("EndEffector");

        Effectors = Effectors.OrderBy(i => i.GetComponent<Effector>().ToolOrder).ToArray();

        SwitchListLength = Effectors.Length;
        ActiveIndex = 1;

        _toolPriorityManager = GetComponent<ToolPriorityManager>();
        _toolPriorityManager.InitPriorityValues();

        UpdateVisibility();

    }

    // Update is called once per frame
    void Update()
    {
        if (_switchTimeRemaining > 0f)
            _switchTimeRemaining -= Time.deltaTime;

        // Simple priority increment with use example
        // TODO: replace with call to new priority manager script
        bool priorityMayHaveChanged = false;
        foreach (GameObject e in Effectors)
        {
            bool used = e.GetComponent<Effector>().CheckToolUseChanged(true);
            if (used)
            {
                e.GetComponent<Effector>().ToolPriority++;
                priorityMayHaveChanged = true;
            }
        }
        // Update switching list
        if (priorityMayHaveChanged && AdaptiveSwitchingEnabled)
        {
            ReorderSwitchListByPriority(true, false);
        }
    }

    // Visibility
    public void UpdateVisibility()
    {
        foreach (GameObject e in Effectors)
        {
            MeshRenderer[] AllMeshRenderers = e.transform.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer m in AllMeshRenderers)
            {
                m.enabled = false;
            }
        }

        GameObject active = Effectors[ActiveIndex];
        MeshRenderer[] MeshRenderers = active.transform.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer m in MeshRenderers)
        {
            if (Effectors[ActiveIndex].GetComponent<Effector>().Attached == true)
                m.enabled = true;
        }

    }


    // Switching Function
    public int Switch()
    {
        if (_switchTimeRemaining <= 0f)
        {
            // If switching list has changed
            // make sure the next thing switched to
            // is the top of the new list
            //if (_switchingListChanged)
            //{
            //    ActiveIndex = SwitchListLength - 1;
            //    _switchingListChanged = false;
            //}
            // Perform switching, looping at end of list
            _switchTimeRemaining = 0.2f;
            ActiveIndex += 1;
            if (ActiveIndex >= SwitchListLength)
            {
                ActiveIndex = 0;
            }
            while (Effectors[ActiveIndex].GetComponent<Effector>().Unlocked == false)
            {
                ActiveIndex += 1;
                if (ActiveIndex >= SwitchListLength)
                {
                    ActiveIndex = 0;
                }
            }
            InteractionAudioSource.PlayOneShot(SwitchingSoundAudio, 0.05f);
        }
        UpdateVisibility();
        return ActiveIndex;
    }

    // Method to change the position of tools in the switching list
    public void ReorderSwitchListByPriority(bool deprioritizeCurrentTool=true, bool priorityIsDefaultToolOrder=false)
    {
        // Stash the unqiue identifier (default tool order position) for the active tool
        int currentToolID = Effectors[ActiveIndex].GetComponent<Effector>().ToolOrder;

        // Use the default ordering set in the editor as the list order
        if (priorityIsDefaultToolOrder)
        {
            Effectors = Effectors.OrderBy(i => i.GetComponent<Effector>().ToolOrder).ToArray();
        }
        // Otherwise, reorder by the priority values for each effector
        else
        {
            // 
            if (deprioritizeCurrentTool)
            {
                Effector e = Effectors[ActiveIndex].GetComponent<Effector>();
                float stashedPriority = e.ToolPriority;
                e.ToolPriority = -1000f;
                Effectors = Effectors.OrderByDescending(i => i.GetComponent<Effector>().ToolPriority).ToArray();
                e.ToolPriority = stashedPriority;
            }
            else
            {
                Effectors = Effectors.OrderByDescending(i => i.GetComponent<Effector>().ToolPriority).ToArray();
            }
        }

        // Preserve the active index in the reordered list
        int i = 0;
        foreach (GameObject e in Effectors)
        {
            if (e.GetComponent<Effector>().ToolOrder == currentToolID)
                ActiveIndex = i;
            i++;
        }
        //_switchingListChanged = true;
    }

    // Toggle between showing and not showing the coming items in the switching list 
    public void VizToggle()
    {
        if (_switchTimeRemaining > 0f)
            return;
        _switchTimeRemaining = 0.2f;

        //TODO: temporary overload to allow switch list reordering
        ReorderSwitchListByPriority(true, false);

        if (!_visualizeList)
        {
            _visualizeList = true;
        }
        else
        {
            _visualizeList = false;
        }
    }

}
