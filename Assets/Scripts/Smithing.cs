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

// This file is responsible for on-anvil object detection and changes to play.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Smithing : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody && other.attachedRigidbody.tag == "GrippableObject" && other.attachedRigidbody.GetComponent<Grippable>() != null)
        {
            Grippable g = other.attachedRigidbody.GetComponent<Grippable>();
            g.OnAnvil = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.attachedRigidbody && other.attachedRigidbody.tag == "GrippableObject" && other.attachedRigidbody.GetComponent<Grippable>() != null)
        {
            Grippable g = other.attachedRigidbody.GetComponent<Grippable>();
            g.OnAnvil = false;
        }
    }



}
