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

// This file is responsible for rendering changes, look, and operation
// of grippable objects (cubes and tool cubes).

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grippable : MonoBehaviour
{

    public GameObject PlacerVolumeVisualObject;
    public GameObject Deform1;
    public GameObject Deform2;
    public ParticleSystem Sparks;
    public int Size;
    public float Heat;
    public Vector3Int Hits;
    public int MaxHitIndex;
    public bool OnAnvil;
    public GameObject FinalForm;
    public GameObject FinalFormInorganic;
    public int TerrainAssetIndex = -1;
    public bool IsFinalForm;
    public bool IsPlaced;
    public bool IsAttached;
    public bool IsOrganic;
    public bool IsAnimal;
    public bool IsTool;
    public string ToolName;
    public int ToolSize;

    private float _heatMax = 10f;
    private Material _material;
    private Material _materialD1;
    private Material _materialD2;
    private MeshRenderer _volumeRenderer;
    private MeshRenderer _deform1Renderer;
    private MeshRenderer _deform2Renderer;

    // Start is called before the first frame update
    void Start()
    {
        _volumeRenderer = PlacerVolumeVisualObject.transform.GetComponent<MeshRenderer>();
        _volumeRenderer.enabled = false;
        _deform1Renderer = Deform1.transform.GetComponent<MeshRenderer>();
        _deform1Renderer.enabled = false;
        _deform2Renderer = Deform2.transform.GetComponent<MeshRenderer>();
        _deform2Renderer.enabled = false;
        _material = GetComponent<Renderer>().material;
        _material.EnableKeyword("_EMISSION");
        _materialD1 = Deform1.GetComponent<Renderer>().material;
        _materialD1.EnableKeyword("_EMISSION");
        _materialD2 = Deform2.GetComponent<Renderer>().material;
        _materialD2.EnableKeyword("_EMISSION");
        //_material.EnableKeyword("_Color");
        OnAnvil = false;
        IsFinalForm = false;
        IsPlaced = false;
        IsAttached = false;
        Hits = new Vector3Int(0, 0, 0);
        if (Sparks != null)
        {
            var emission = Sparks.emission;
            emission.enabled = true;
        }
    }


    public bool Deform1Enabled
    {
        get
        {
            return _deform1Renderer.enabled;
        }
        set
        {
            _deform1Renderer.enabled = value;
        }
    }

    public bool Deform2Enabled
    {
        get
        {
            return _deform2Renderer.enabled;
        }
        set
        {
            _deform2Renderer.enabled = value;
        }
    }

    // Update is called once per frame
    void Update()
    {
        Heat -= 0.001f;
        if (Heat > _heatMax)
            Heat = _heatMax;
        if (Heat < 0)
            Heat = 0f;

        if (IsTool)
        {
            _material.SetColor("_EmissionColor", new Vector4(0f, Mathf.Max(0f, 1.2f * Heat / 5f - 1f), 1.2f * Heat / 5.0f));
            _materialD1.SetColor("_EmissionColor", new Vector4(0f, Mathf.Max(0f, 1.2f * Heat / 5f - 1f), 1.2f * Heat / 5.0f));
            _materialD2.SetColor("_EmissionColor", new Vector4(0f, Mathf.Max(0f, 1.2f * Heat / 5f - 1f), 1.2f * Heat / 5.0f));
        }
        else
        {
            _material.SetColor("_EmissionColor", new Vector4(1.2f * Heat / 5.0f, Mathf.Max(0f, 1.2f * Heat / 5f - 1f), 0f));
            _materialD1.SetColor("_EmissionColor", new Vector4(1.2f * Heat / 5.0f, Mathf.Max(0f, 1.2f * Heat / 5f - 1f), 0f));
            _materialD2.SetColor("_EmissionColor", new Vector4(1.2f * Heat / 5.0f, Mathf.Max(0f, 1.2f * Heat / 5f - 1f), 0f));
            //_material.SetColor("_Color", new Vector4(Heat / 5.0f, Mathf.Max(0f, Heat / 5f - 1f), 0f));
        }

        if (IsPlaced)// && !IsAttached)
        {
            _volumeRenderer.enabled = true;
            _volumeRenderer.transform.rotation = Quaternion.identity;
            Transform final = transform.Find("FinalForm");
            if (final) {
                Vector3 pos = transform.position;
                pos.x = transform.position.x;
                pos.z = transform.position.z;
                pos.y = final.position.y - 1f;
                final.rotation = Quaternion.identity;
                _volumeRenderer.transform.position = pos;
                //_volumeRenderer.transform.localPosition
                //     = new Vector3(0f, _volumeRenderer.transform.position.y - 2f, 0f);
            }

        }
        else
        {
            _volumeRenderer.enabled = false;
        }


    }

    private void FixedUpdate()
    {
        Collider[] overlapingColliders = Physics.OverlapBox(gameObject.transform.position, new Vector3(0.001f, 0.001f, 0.001f), Quaternion.identity);
        if (!IsAttached && !IsPlaced && overlapingColliders.Length > 1)
        {
            int i = 0;
            while (i < overlapingColliders.Length)
            {
                Collider c = overlapingColliders[i];
                if (!c.isTrigger && c.tag == "SolidObject")
                    this.GetComponent<Rigidbody>().AddForce(Vector3.up * 10);
                i++;
            }
        }
    }

    void LateUpdate()
    {
        // Check if objects is below any playable terrains
        // And correct so this item does not get lost forever
        // (Possible game breaking if a tool, for example)
        if (this.GetComponent<Rigidbody>().isKinematic)
            return; // Check if held or otherwise process controlled
        foreach (Terrain t in Terrain.activeTerrains)
        {
            if (t.gameObject.tag == "Terrain")
            {
                Vector3 gPos = transform.position;
                Bounds bounds = new Bounds(t.terrainData.bounds.center
                    + t.transform.position,
                    t.terrainData.bounds.size);
                if (bounds.Contains(gPos))
                {
                    float posY = gPos.y;
                    float tPosY = t.SampleHeight(gPos) + t.GetPosition().y;
                    transform.position = new Vector3(transform.position.x, Mathf.Max(transform.position.y, tPosY), transform.position.z);
                }
            }
        }
    }


}
