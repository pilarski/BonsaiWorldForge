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

// This file is responsible for detecting interactions with the bonsai
// container and modifying grippable objects accordingly. 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlacementRegion : MonoBehaviour
{

    public BonsaiTerrainPlacement BonsaiPlaceComponent;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody.tag == "GrippableObject" && other.attachedRigidbody.GetComponent<Grippable>().IsFinalForm)
        {
            other.attachedRigidbody.GetComponent<Grippable>().IsPlaced = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.attachedRigidbody.tag == "GrippableObject" && other.attachedRigidbody.GetComponent<Grippable>().IsFinalForm)
        {
            other.attachedRigidbody.GetComponent<Grippable>().IsPlaced = false;
        }

    }

    private void OnTriggerStay(Collider other)
    {
        if (other.attachedRigidbody.GetComponent<Grippable>() != null
            && other.attachedRigidbody.GetComponent<Grippable>().IsPlaced
            && other.transform.parent == null)
        {
            other.attachedRigidbody.GetComponent<Grippable>().IsAttached = true;
            other.attachedRigidbody.gameObject.tag = "Untagged";
            other.attachedRigidbody.isKinematic = true;
            BonsaiPlaceComponent.ObjectToAttach = other.attachedRigidbody.gameObject;
        }
    }


}
