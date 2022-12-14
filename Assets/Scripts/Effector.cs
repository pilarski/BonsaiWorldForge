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

// This file is responsible for end effector component elements.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Effector : MonoBehaviour
{

    public string Type;
    public int Size;
    public int ToolOrder; // Used as a unqiue tool ID; TODO: rename?
    public float ToolPriority;
    public int ToolUseCount;
    public bool ToolUseCountChanged;
    public bool Unlocked = true;
    public bool Attached = true;

    // Start is called before the first frame update
    void Start()
    {
        ToolPriority = 0f;
        ToolUseCount = 0;
        ToolUseCountChanged = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Methos to flag that tool was used
    public void ToolWasUsed()
    {
        ToolUseCount++;
        ToolUseCountChanged = true;
    }

    // Method to query for new tool use and resetthe use flag
    public bool CheckToolUseChanged(bool reset = true)
    {
        bool changed = ToolUseCountChanged;
        if (reset)
        {
            ToolUseCountChanged = false;
        }
        return changed;
    }
}