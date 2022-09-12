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

// This file is responsible for all core game progression, including play 
// changes due to terraform activity, and checking for game completion.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class TerraformControl : MonoBehaviour
{

    public int TerraformIndex;
    public int TerraformIndexOrganic;
    public int TerraformIndexInorganic;
    public int TerraformIndexAquatic;
    public int TerraformIndexAnimal;
    public Vector3Int TerraformIndexSizes = new Vector3Int(0, 0, 0);
    public Vector3Int TerraformIndexOrganicSizes = new Vector3Int(0, 0, 0);
    public Vector3Int TerraformIndexInorganicSizes = new Vector3Int(0, 0, 0);
    public Vector3Int TerraformIndexAquaticSizes = new Vector3Int(0, 0, 0);
    public GameObject[] OrganicBasedAnimals;
    public int[] OrganicBasedAnimalsCountMod;
    public Vector3[] OrganicBasedAnimalsPositions;
    public GameObject[] InorganicBasedAnimals;
    public int[] InorganicBasedAnimalsCountMod;
    public Vector3[] InorganicBasedAnimalsPositions;
    public GameObject[] AquaticBasedAnimals;
    public int[] AquaticBasedAnimalsCountMod;
    public Vector3[] AquaticBasedAnimalsPositions;
    public bool TerraformIndexChanged;

    private Terrain _terraformedTerrain;
    public Vector3 TerraformedTerrainOrigin;
    public Vector3 TerraformedTerrainSize;

    public Material CaveSystemWallMaterial;
    public Material IslandCoreMaterial;
    public GameObject CoreLightSource;
    public GameObject CommsUplinkGlow;
    public GameObject ControlDashboard;
    public GameObject TransportRing;
    public GameObject TransportField;

    public float EnergyLevel;
    public float IndextoEnergyConversionRatio;
    public bool CommsUplinkActive;
    public bool PlayerInTransportField;
    public bool PlayerExitedMap;
    public Text ExitText;

    [ColorUsage(true, true)]
    private Color _coreMaterialColor;
    [ColorUsage(true, true)]
    private Color _coreMaterialEmissionColor;
    [ColorUsage(true, true)]
    private Color _coreMaterialColorInit = new Color(0f, 0f, 0f);
    private Light _coreLight;
    private Material _controlDashboardMaterial;
    private Material _transportRingMaterial;

    private Vector3Int _lastTerraformIndexOrganicSizes = new Vector3Int(0, 0, 0);
    private Vector3Int _lastTerraformIndexInorganicSizes = new Vector3Int(0, 0, 0);
    private Vector3Int _lastTerraformIndexAquaticSizes = new Vector3Int(0, 0, 0);

    // Game completion goal ammounts
    private int _goalTI = 10;
    private int _goalIO = 1;
    private int _goalO = 1;
    private int _goalAQ = 1;
    private int _goalAN = 1;
    private int _goalS = 1;
    private int _goalM = 1;
    private int _goalL = 1;


    // Start is called before the first frame update
    void Start()
    {
        EnergyLevel = 0f;
        CommsUplinkActive = false;
        TerraformIndexChanged = false;
        _coreMaterialColor = _coreMaterialColorInit;

        _controlDashboardMaterial = ControlDashboard.GetComponent<Renderer>().material;
        _transportRingMaterial = TransportRing.GetComponent<Renderer>().material;
        MeshRenderer commsGlowRenderer = CommsUplinkGlow.transform.GetComponent<MeshRenderer>();
        commsGlowRenderer.enabled = false;

        MeshRenderer transportFieldRenderer = TransportField.transform.GetComponent<MeshRenderer>();
        transportFieldRenderer.enabled = false;

        _coreLight = CoreLightSource.GetComponent<Light>();
        ExitText.enabled = false;

        // If in low-quality mode, boost lightining and add colour
        // to simulate effect of lost reflections and probes
        int quality = QualitySettings.GetQualityLevel();
        if (quality == 0)
        {
            RenderSettings.ambientLight = new Color(0.6f, 0.7f, 0.7f);
        }

    }

    // Update is called once per frame
    void Update()
    {
        // Check for changes that might intantiate animals
        if (TerraformIndexChanged)
            CheckAnimalInstantiation();

        // Check if environment is complete (move into above IF once done debugging TODO)
        CheckCompletion();
        CheckMapExit();

        // Set energy level based on current terraform level
        EnergyLevel = (float)TerraformIndex * IndextoEnergyConversionRatio;
        var lightLevel = EnergyLevel - 3f + _coreLight.intensity;

        // Compute new wall colour
        _coreMaterialColor = new Vector4(1.2f * lightLevel / 5.0f, Mathf.Max(0f, 1.2f * lightLevel / 5f - 1f), Mathf.Max(0f, 1.2f * lightLevel / 5f - 6f));
        CaveSystemWallMaterial.SetColor("_EmissionColor", _coreMaterialColor);
        IslandCoreMaterial.SetColor("_EmissionColor", _coreMaterialColor);

        // Visualize comms uplink when appropriate
        MeshRenderer commsGlowRenderer = CommsUplinkGlow.transform.GetComponent<MeshRenderer>();
        if (CommsUplinkActive)
            commsGlowRenderer.enabled = true;
        else
            commsGlowRenderer.enabled = false;


    }

    void CheckCompletion()
    {
        float intensity = 5f;

        // Visually signal that the game soft completion criteria met
        if (TerraformIndex >= _goalTI
            && TerraformIndexOrganic >= _goalO
            && TerraformIndexInorganic >= _goalIO
            && TerraformIndexAquatic >= _goalAQ
            && TerraformIndexAnimal >= _goalAN
            && TerraformIndexSizes[0] >= _goalS
            && TerraformIndexSizes[1] >= _goalM
            && TerraformIndexSizes[2] >= _goalL
            )
        {
            CommsUplinkActive = true;
        }

        // Light up final parts of control room
        if (CommsUplinkActive)
        { 
            _controlDashboardMaterial.SetFloat("ActiveUplink", intensity);
            MeshRenderer transportFieldRenderer = TransportField.transform.GetComponent<MeshRenderer>();
            transportFieldRenderer.enabled = true;
            _transportRingMaterial.EnableKeyword("_EMISSION");
            _transportRingMaterial.SetColor("_EmissionColor", new Vector4(0.5f * intensity, Mathf.Max(0f, intensity - 1f), intensity));
        }

        // Update control panel for each subgoal completed
        if (TerraformIndex >= _goalTI)
            _controlDashboardMaterial.SetFloat("ActiveTI", intensity);
        if (TerraformIndexInorganic >= _goalIO)
            _controlDashboardMaterial.SetFloat("ActiveIO", intensity);
        if (TerraformIndexOrganic >= _goalO)
            _controlDashboardMaterial.SetFloat("ActiveO", intensity);
        if (TerraformIndexAquatic >= _goalAQ)
            _controlDashboardMaterial.SetFloat("ActiveAQ", intensity);
        if (TerraformIndexAnimal >= _goalAN)
            _controlDashboardMaterial.SetFloat("ActiveAN", intensity);
        if (TerraformIndexSizes[0] >= _goalS)
            _controlDashboardMaterial.SetFloat("ActiveS", intensity);
        if (TerraformIndexSizes[1] >= _goalM)
            _controlDashboardMaterial.SetFloat("ActiveM", intensity);
        if (TerraformIndexSizes[2] >= _goalL)
            _controlDashboardMaterial.SetFloat("ActiveL", intensity);


    }

    void CheckMapExit()
    {
        if (CommsUplinkActive)
            // check here for player having entered the transport field
            if (PlayerInTransportField)
            {
                PlayerExitedMap = true;
                ExitText.enabled = true;
            }
            
    }

    void CheckAnimalInstantiation()
    {
        // ORGANIC: Check for changes in different sizes and types
        // If WebGL, spawn birds instead of Lizzos
        bool WebGLOverrideOrganic = _lastTerraformIndexInorganicSizes != TerraformIndexInorganicSizes;
        if (Application.platform != RuntimePlatform.WebGLPlayer)
            WebGLOverrideOrganic = false; 
        if (_lastTerraformIndexOrganicSizes != TerraformIndexOrganicSizes || WebGLOverrideOrganic)
        {
            // Check for small flock, medium flock, large flock
            for (int i = 0; i < 3; i++)
            {
                bool WebGLOverride = _lastTerraformIndexInorganicSizes[i] != TerraformIndexInorganicSizes[i]
                       && TerraformIndexInorganicSizes[i] % InorganicBasedAnimalsCountMod[i] == 0
                        && TerraformIndexInorganicSizes[i] != 0;
                if (Application.platform != RuntimePlatform.WebGLPlayer)
                    WebGLOverride = false; 
                if ((_lastTerraformIndexOrganicSizes[i] != TerraformIndexOrganicSizes[i]
                    && TerraformIndexOrganicSizes[i] % OrganicBasedAnimalsCountMod[i] == 0
                    && TerraformIndexOrganicSizes[i] != 0)
                    || WebGLOverride)
                {
                    GameObject newAnimals = null;
                    TerraformIndexAnimal += 1;

                    // Create animal group from prefab
                    newAnimals = Instantiate(OrganicBasedAnimals[i]) as GameObject;

                    // Define XZ circle for placement scaled by component-set value
                    Vector3 unitCircle = Random.insideUnitCircle * OrganicBasedAnimalsPositions[i][0];

                    // Create placement position from circle
                    Vector3 pos = new Vector3(
                        TerraformedTerrainOrigin[0] + TerraformedTerrainSize[0] / 2.0f + unitCircle[0],
                        OrganicBasedAnimalsPositions[i][1],
                        TerraformedTerrainOrigin[2] + TerraformedTerrainSize[2] / 2.0f + unitCircle[1]);
                    newAnimals.transform.position = pos;

                    // Set position if prefab parent is animal
                    if (newAnimals.TryGetComponent(out BirdAnimation baMain))
                        baMain.CentrePointOfMotion = pos;

                    // Set position of prefab children animals
                    foreach (Transform a in newAnimals.transform) {
                        if (a.TryGetComponent(out BirdAnimation ba))
                            ba.CentrePointOfMotion = pos;
                    }
                }
            }
        }

        // INORGANIC: Check for changes in different sizes and types
        if (_lastTerraformIndexInorganicSizes != TerraformIndexInorganicSizes)
        {
            // Don't place Lizzo if no nav mesh due to WebGL build
            if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                // Check for small lounge, medium lounge, large lounge
                for (int i = 0; i < 3; i++)
                {
                    if (_lastTerraformIndexInorganicSizes[i] != TerraformIndexInorganicSizes[i]
                        && TerraformIndexInorganicSizes[i] % InorganicBasedAnimalsCountMod[i] == 0
                        && TerraformIndexInorganicSizes[i] != 0)
                    {
                        GameObject newAnimals = null;
                        TerraformIndexAnimal += 1;

                        // Spawn more animlas if they are smaller prefabs
                        for (int n = 0; n < (3 - i); n++)
                        {
                            // Define XZ circle for placement scaled by component-set value
                            Vector3 unitCircle = Random.insideUnitCircle * InorganicBasedAnimalsPositions[i][0];

                            // Create placement position from circle
                            Vector3 pos = new Vector3(
                                TerraformedTerrainOrigin[0] + TerraformedTerrainSize[0] / 2.0f + unitCircle[0],
                                InorganicBasedAnimalsPositions[i][1],
                                TerraformedTerrainOrigin[2] + TerraformedTerrainSize[2] / 2.0f + unitCircle[1]);
                            pos = new Vector3(pos[0], MaxTerrainHeight(pos), pos[2]);

                            // Check for spot on navmesh to instantiate in valid position
                            NavMeshHit hit;
                            NavMesh.SamplePosition(pos, out hit, 10f, NavMesh.AllAreas);

                            // Create animal group from prefab
                            newAnimals = Instantiate(InorganicBasedAnimals[i], hit.position, Quaternion.identity) as GameObject;


                            // Set position if prefab parent is animal
                            if (newAnimals.TryGetComponent(out LizzoNavPlusAnimation baMain))
                                baMain.CentrePointOfMotion = pos;

                            // Set position of prefab children animals
                            foreach (Transform a in newAnimals.transform)
                            {
                                if (a.TryGetComponent(out LizzoNavPlusAnimation ba))
                                    ba.CentrePointOfMotion = pos;
                            }
                        }
                    }
                }
            }
        }

        // AQUATIC: Check for changes in different sizes and types
        if (_lastTerraformIndexAquaticSizes != TerraformIndexAquaticSizes)
        {
            GameObject newAnimals = null;
            // Check for small shoal, medium shoal, large shoal
            for (int i = 0; i < 3; i++)
            {
                if (_lastTerraformIndexAquaticSizes[i] != TerraformIndexAquaticSizes[i]
                    && TerraformIndexAquaticSizes[i] % AquaticBasedAnimalsCountMod[i] == 0
                    && TerraformIndexAquaticSizes[i] != 0)
                {
                    // Create animal group from prefab
                    newAnimals = Instantiate(AquaticBasedAnimals[i]) as GameObject;

                    // How many times should we try to place the animal?
                    int trys = 250;

                    // Propose positions and check for invalid placement
                    for (int t = 0; t < trys; t++)
                    {
                        // Define XZ circle for placement scaled by component-set value
                        Vector3 unitCircle = Random.insideUnitCircle * AquaticBasedAnimalsPositions[i][0];

                        // Create placement position from circle
                        Vector3 pos = new Vector3(
                         TerraformedTerrainOrigin[0] + TerraformedTerrainSize[0] / 2.0f + unitCircle[0],
                         AquaticBasedAnimalsPositions[i][1],
                         TerraformedTerrainOrigin[2] + TerraformedTerrainSize[2] / 2.0f + unitCircle[1]);
                        newAnimals.transform.position = pos;

                        // Check terrain heights and re-roll placement if under
                        if (!CheckForUndergroundPlacement(pos, AquaticBasedAnimalsPositions[i][2]))
                        {
                            Debug.Log("Failed placement" + t);
                            continue;
                        }

                        // Set position if prefab parent is animal
                        if (newAnimals.TryGetComponent(out BoxFishAnimation baMain))
                            baMain.CentrePointOfMotion = pos;

                        // Set position of prefab children animals
                        foreach (Transform a in newAnimals.transform)
                        {
                            if (a.TryGetComponent(out BoxFishAnimation ba))
                                ba.CentrePointOfMotion = pos;
                        }

                        // Break loop if position found
                        TerraformIndexAnimal += 1;
                        break;
                    }
                }
            }
        }

        // Reset change flag
        TerraformIndexChanged = false;

        // Update last values
        _lastTerraformIndexOrganicSizes = TerraformIndexOrganicSizes;
        _lastTerraformIndexInorganicSizes = TerraformIndexInorganicSizes;
        _lastTerraformIndexAquaticSizes = TerraformIndexAquaticSizes;
    }

    bool CheckForUndergroundPlacement(Vector3 pos, float threshold)
    {

        foreach (Terrain t in Terrain.activeTerrains)
        {
            if (t.gameObject.tag == "Terrain")
            {
                Bounds bounds = new Bounds(t.terrainData.bounds.center
                    + t.transform.position,
                    t.terrainData.bounds.size);
                if (bounds.Contains(pos))
                {
                    float posY = pos.y;
                    float tPosY = t.SampleHeight(pos) + t.GetPosition().y;
                    if (posY - threshold < tPosY)
                        return false;
                }
            }
        }
        return true;
    }


    float MaxTerrainHeight(Vector3 pos)
    {
        float max = -1000f;
        foreach (Terrain t in Terrain.activeTerrains)
        {
            if (t.gameObject.tag == "Terrain")
            {
                Bounds bounds = new Bounds(t.terrainData.bounds.center
                    + t.transform.position,
                    t.terrainData.bounds.size);
                if (bounds.Contains(pos))
                {
                    float posY = pos.y;
                    float tPosY = t.SampleHeight(pos) + t.GetPosition().y;
                    if (max < tPosY)
                        max = tPosY;
                }
            }
        }
        return max;
    }

}
