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

// This file is responsible for creating procedural terrain
// and placing assets on the terrain at runtime.


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BonsaiTerrainPlacement : MonoBehaviour
{

    public TerrainData TerrainDataToClone;
    public AudioSource InteractionAudioSource;
    public AudioClip PlacementAudio;
    public int[] TerrainPerlinSeed = new int[2];
    public bool RandmonizeTerrainMap = false;

    public int IndexofOranicProto1;
    public int IndexofOranicProto2;
    public int IndexofOranicProto3;
    public int IndexofInoranicProto1;
    public int IndexofInoranicProto2;
    public int IndexofInoranicProto3;
    public int IndexofDetailOranicProto1;
    public int IndexofDetailOranicProto2;
    public int IndexofDetailOranicProto3;
    public int IndexofDetailInoranicProto1;
    public int IndexofDetailInoranicProto2;
    public int IndexofDetailInoranicProto3;
    public int IndexofAquaticProto1;
    public int IndexofAquaticProto2;

    public int IndexofSandTexLayer;
    public int IndexofGrassTexLayer;
    public int IndexofRockTexLayer;
    public int IndexofSeaBottomTexLayer;
    public int IndexofSeaGrassTexLayer;

    public GameObject ObjectToAttach;

    private TerrainData _microTerrainData;
    private TerrainData _macroTerrainData;
    public Terrain _microTerrain;
    public Terrain _macroTerrain;
    public NavMeshSurface[] NavSurfaces;

    private float _waterline;
    private float _maxHillHeight;
    private int _numSplatMaps = 4;

    private TerraformControl _terraformControl;

    // Start is called before the first frame update
    void Start()
    {
        // Set perlin seed for heights randomly?
        // Sane seed values are [100,100]
        if (RandmonizeTerrainMap)
        {
            TerrainPerlinSeed[0] = Random.Range(0, 1000);
            TerrainPerlinSeed[1] = Random.Range(0, 1000);
        }
        
        // Set max height of top of terrain
        _maxHillHeight = 300f;
        
        // Use Terrain Clone to copy base terrain and layers
        _microTerrainData = TerrainDataCloner.Clone(TerrainDataToClone, _microTerrain.terrainData);
        GenerateHeights(_microTerrainData, 4, _maxHillHeight, 0, 0, 0);
        _waterline = 0.35f;
        GenerateTerrainLayers(_microTerrainData, _maxHillHeight, 0.94f, 0.45f, 1.15f, _waterline);

        _macroTerrainData = TerrainDataCloner.Clone(_microTerrainData, _macroTerrain.terrainData);
        _microTerrainData.size = new Vector3(3f, 1f, 3f);
        _macroTerrainData.size = new Vector3(60f, 20f, 60f);

        // Generate actual game object to attach to container
        _microTerrain.terrainData = _microTerrainData;
        _microTerrain.transform.parent = this.transform;
        _microTerrain.transform.position = this.transform.position + new Vector3(-1.5f,-0.2f,-1.5f);

        _macroTerrain.terrainData = _macroTerrainData;
        _macroTerrain.transform.parent = this.transform;
        _macroTerrain.transform.position = this.transform.position + new Vector3(-40f, -7f, 20f);
        _macroTerrain.tag = "Terrain";


        // Realtime bake nav surfaces (relies on preview Unity packages)
        // Disable NaVMesh and Lizzo when on WebGL to speed up loadtime
        if (Application.platform != RuntimePlatform.WebGLPlayer)
        {
            NavMeshSurface[] surfaces = NavSurfaces;
            for (int i = 0; i < surfaces.Length; i++)
            {
                surfaces[i].BuildNavMesh();
            }
        }

        ObjectToAttach = null;

        _terraformControl = FindObjectOfType<TerraformControl>();
        _terraformControl.TerraformedTerrainOrigin = _macroTerrain.transform.position;
        _terraformControl.TerraformedTerrainSize = _macroTerrainData.size;
    }

    // Generate height map that also stays within a small bound of outside of the region
    public void GenerateHeights(TerrainData terrainData, float tileSize, float highest, float lowest, int xbound, int ybound)
    { 
        float HighestHillHeight = highest;
        float LowestHillHeight = lowest;
        float hillHeight = (float)((float)HighestHillHeight - (float)LowestHillHeight) / ((float)terrainData.heightmapResolution / 2);
        float baseHeight = (float)LowestHillHeight / ((float)terrainData.heightmapResolution / 2);
        float[,] heights = new float[terrainData.heightmapResolution, terrainData.heightmapResolution];

        for (int i = xbound; i < terrainData.heightmapResolution - xbound; i++)
        {
            for (int k = ybound; k < terrainData.heightmapResolution- ybound; k++)
            {
                float radiusScale = 1.0f - Mathf.Sqrt(Mathf.Pow(Mathf.Abs(terrainData.heightmapResolution / 2.0f - i), 2.0f)
                    + Mathf.Pow(Mathf.Abs(terrainData.heightmapResolution / 2.0f - k), 2)) / (0.5f * terrainData.heightmapResolution);
                heights[i, k] = baseHeight + radiusScale
                    * (Mathf.PerlinNoise(((float)(i + TerrainPerlinSeed[0]) / (float)terrainData.heightmapResolution)
                    * tileSize, ((float)(k + TerrainPerlinSeed[1]) / (float)terrainData.heightmapResolution)
                    * tileSize) * (float)hillHeight);
            }
        }
        terrainData.SetHeights(0, 0, heights);
    }


    // Generate layer map that also stays within a small bound of outside of the region
    public void GenerateTerrainLayers(TerrainData terrainData, float maxHeight, float l1Cutoff, float l2Cutoff, float l3Cutoff, float l4Cutoff)
    {
        float[,,] map = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, _numSplatMaps];

        // For each point on the alphamap...
        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                // Get teerain heaight via normalized cooridnates
                float normX = x * 1.0f / (terrainData.alphamapWidth - 1);
                float normY = y * 1.0f / (terrainData.alphamapHeight - 1);
                var height = terrainData.GetInterpolatedHeight(normY, normX);
                var scaledHeight = height / maxHeight;

                // Set and blend layer strengths accoring to height
                // Vary using Perlin noise
                var layer1strength = 1.0f * (1.0f - Mathf.Pow(Mathf.Abs(scaledHeight-0.5f), 1f));
                var layer2strength = 0f;
                var layer3strength = 0f;
                var layer4strength = 0f;
                if (scaledHeight < l2Cutoff - (0.1f * Mathf.PerlinNoise(y / 12f, x / 12f)))
                    layer1strength = 0.5f;
                if (scaledHeight < l2Cutoff + (0.2f * Mathf.PerlinNoise(y / 20f, x / 20f))
                    && scaledHeight > l4Cutoff - 0.05f + (0.05f * Mathf.PerlinNoise(y / 15f, x / 15f)))
                    layer2strength = 1.0f - Mathf.PerlinNoise(y / 8f, x / 8f) ;
                if (scaledHeight > l3Cutoff - (0.3f * Mathf.PerlinNoise(y / 10f, x / 10f)))
                    layer3strength = ((1.0f - Mathf.PerlinNoise(y + 100f, x + 100f)) )* Mathf.Pow(scaledHeight,3);
                if (scaledHeight < l4Cutoff + (0.2f * Mathf.PerlinNoise(y / 7f, x / 7f)))
                    layer4strength = 1.0f - Mathf.PerlinNoise(y + 50f, x + 50f);
                if (scaledHeight < l4Cutoff - 0.1f - 0.15f * Mathf.PerlinNoise(y / 5f, x / 5f))
                {
                    layer1strength = scaledHeight-0.05f;
                    layer2strength = scaledHeight-0.05f;
                    layer3strength = scaledHeight-0.05f;
                    layer4strength = 1.0f;
                }

                map[x, y, 0] = (float)layer1strength;
                map[x, y, 1] = (float)layer2strength;
                map[x, y, 2] = (float)layer3strength;
                map[x, y, 3] = (float)layer4strength;
            }
        }
        terrainData.SetAlphamaps(0, 0, map);
    }


    public void PlaceLayerSplat(TerrainData terrainData, int layer, Vector3 pos, float radius, int numlayers)
    {
        float[,,] map = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);

        // For each point on the alphamap near the placed position
        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                for (int z = 0; z < numlayers; z++) {
                    float distsqr = Mathf.Sqrt(Mathf.Pow(y - (int)(pos[0] * terrainData.alphamapHeight), 2) + Mathf.Pow(x - (int)(pos[2] * terrainData.alphamapWidth), 2));
                    float magnitude = 0.35f - Mathf.Pow(distsqr, 2.5f - radius * 0.25f) / ((float)terrainData.alphamapHeight) + 0.7f * Mathf.PerlinNoise(y / 10f, x / 10f); //1.8f
                    if (magnitude >= 0f)
                    {
                        if (z == layer)
                        {
                            map[x, y, z] += magnitude * (0.6f + radius * 0.1f);
                        }
                        else
                        {
                            map[x, y, z] -= magnitude * (0.6f + radius * 0.1f);
                        }
                    }

                }
            }
        }
        terrainData.SetAlphamaps(0, 0, map);
    }

    public void PlaceDetails(TerrainData terrainData, int layer, Vector3 pos, float radius, float densityProbability)
    {
        int[,] map = terrainData.GetDetailLayer(0, 0, terrainData.detailWidth, terrainData.detailHeight, layer);
        terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);

        // For each point on the alphamap near the placed position
        for (int y = 0; y < terrainData.detailHeight; y++)
        {
            for (int x = 0; x < terrainData.detailWidth; x++)
            {
                    float distsqr = Mathf.Sqrt(Mathf.Pow(y - (int)(pos[0] * terrainData.detailHeight), 2) + Mathf.Pow(x - (int)(pos[2] * terrainData.detailWidth), 2));
                    float magnitude = 0.35f - Mathf.Pow(distsqr, 2.5f - radius * 0.25f) / ((float)terrainData.detailHeight) + 0.7f * Mathf.PerlinNoise(y / 10f, x / 10f); //1.8f
                    if (magnitude >= 0f)
                    {
                        if (Random.value < densityProbability)
                            map[x, y] += 1;                 
                    }
            }
        }
        terrainData.SetDetailLayer(0, 0, layer, map);
    }


    // Attach a final-form object once placed
    public void Attach(GameObject other)
    {
        InteractionAudioSource.PlayOneShot(PlacementAudio, 0.1f);
        other.transform.parent = _microTerrain.transform;
        Vector3 pos = other.transform.localPosition;
        int index = other.GetComponent<Grippable>().TerrainAssetIndex;
        Vector3 hits = other.GetComponent<Grippable>().Hits;
        int maxindex = other.GetComponent<Grippable>().MaxHitIndex;
        Terrain terrain = _macroTerrain.GetComponent<Terrain>();
        int groundtex = 1;

        // Update Terraform Control statistics
        _terraformControl.TerraformIndex += 1;
        _terraformControl.TerraformIndexChanged = true;
        if (other.GetComponent<Grippable>().IsOrganic)
        {
            _terraformControl.TerraformIndexOrganic += 1;
            _terraformControl.TerraformIndexOrganicSizes[maxindex - 1] += 1;
        }
        else if (!other.GetComponent<Grippable>().IsOrganic)
        {
            _terraformControl.TerraformIndexInorganic += 1;
            _terraformControl.TerraformIndexInorganicSizes[maxindex - 1] += 1;
        }
        _terraformControl.TerraformIndexSizes[maxindex - 1] += 1;

        // Scale the size and number of tree instances buy the distribution of hammer hits
        float size = 0.01f;
        if (index == 0) // correct for small palm tree mesh
            size = 0.15f;
        if (index > 2) // correct for small boulder mesh and change terrain texture
        {
            size = 0.1f;
            groundtex = 2;
        }
        int nummin = 1;
        int nummax = 1;
        size = 0.1f * (float)(maxindex * 2);
        if (index > 2 && maxindex == 3)
            size *= 2f;
        nummin = 4 - maxindex;
        nummax = 7 - 2 * maxindex;
        if (index > 2 && maxindex < 3)
        {
            // Increase the number of smaller boulder objects
            nummax *= 2;
            nummin *= 2;
        }

        // Generate trees in a radius
        float placeRadius = 0.2f;
        if (maxindex == 3)
            placeRadius = 0.0f; // unless it is one big object, then place at specified location

        // Create tree instances at the selected point
        Vector3 scaledPos = new Vector3(pos[0]/3.0f,0,pos[2]/3.0f);
        bool aquatic = MakeTreeInstances(terrain, scaledPos, Random.Range(nummin, nummax), index, size, placeRadius, maxindex, groundtex, true);
        if (aquatic)
        {
            // Note: aquatic and non-aquatic animals may spawn at same time
            // This is maybe okay, as it is generous and spawns more animals
            // But could check if a non-aquatic was also spawned
            _terraformControl.TerraformIndexAquaticSizes[maxindex - 1] += 1;
        }
    }


    private bool MakeTreeInstances(Terrain terrain, Vector3 pos, int num, int index, float size, float placementRadius, int maxindex, int groundtex, bool applySplat)
    {
        bool aquaticPlaced = false;
        for (int i = 0; i < num; i++)
        {
            int groundtexToApply = groundtex;
            // Create a new tree to add to the terrain
            TreeInstance tree = new TreeInstance();
            tree.prototypeIndex = index;
            tree.color = Color.white;
            tree.lightmapColor = Color.white;
            tree.heightScale = size + Random.value * 0.1f;
            tree.widthScale = size + Random.value * 0.1f;
            tree.position = pos + new Vector3(Random.value * placementRadius
                - placementRadius / 2f, 0, Random.value * placementRadius
                - placementRadius / 2f);
            tree.rotation = Random.Range(0, 2 * Mathf.PI);
            int detailGrassType = 0;

            // Check if tree is below water line; if so, swap for sea grass
            // Remeber height query is (Y,X) not (X,Y)
            float treeHeightonTerrain = terrain.terrainData.GetInterpolatedHeight(tree.position[0], tree.position[2]);
            if (treeHeightonTerrain < 3.3f) // Height of waterline is ~3m
            {
                if (applySplat) // Only change index for main plant
                {
                    _terraformControl.TerraformIndexAquatic += 1;
                    //_terraformControl.TerraformIndexAquaticSizes[maxindex - 1] += 1;
                    aquaticPlaced = true;
                }
                detailGrassType = 2;
                tree.prototypeIndex = IndexofAquaticProto1; // Tidezone undersea plant
                if (treeHeightonTerrain < 2f)
                    if (Random.value < 0.5) // Increase diversity deeper in ocean
                        tree.prototypeIndex = IndexofAquaticProto2; // Deep undersea plant
                groundtexToApply = IndexofSeaGrassTexLayer;
            }
            // Add this tree
            terrain.AddTreeInstance(tree);

            // Check if details should be applied around this tree
            // Is so, populate area around tree with smaller tree objects
            if (applySplat)
            {
                // Assign starting tree prototype index for smaller details
                int detailIndex = IndexofDetailOranicProto1;
                int detailIndex2 = IndexofDetailOranicProto2;
                int detailIndex3 = IndexofDetailOranicProto3;
                // Check to see if prototypes should be organic (protos 6-8)
                // or inorganic (protos 9-11)
                if (index > 2)
                {
                    detailIndex = IndexofDetailInoranicProto1;
                    detailIndex2 = IndexofDetailInoranicProto2;
                    detailIndex3 = IndexofDetailInoranicProto3;
                    if (detailGrassType != 2)
                        detailGrassType = 1;
                }

                // Create small groups of three different detail assets
                MakeTreeInstances(terrain, tree.position, Random.Range(maxindex-1, maxindex + 1), detailIndex, 0.05f, 0.05f * maxindex, maxindex, -1, false);
                MakeTreeInstances(terrain, tree.position, Random.Range(maxindex-1, maxindex + 1), detailIndex2, 0.05f, 0.05f * maxindex, maxindex, -1, false);
                MakeTreeInstances(terrain, tree.position, Random.Range(maxindex-1, maxindex + 1), detailIndex3, 0.05f, 0.05f * maxindex, maxindex, -1, false);

                // Make the tree-adjacent textures align to the tree via splatmap
                PlaceLayerSplat(terrain.terrainData, groundtexToApply, tree.position, (float)maxindex, _numSplatMaps);
                PlaceDetails(terrain.terrainData, detailGrassType, tree.position, (float)maxindex * 1.5f, 0.1f);

            }
        }
        // Apply changes to the terrain
        terrain.Flush();

        return aquaticPlaced;
    }

    // Update is called once per frame
    void Update()
    {
        if (ObjectToAttach != null)
        {
            Attach(ObjectToAttach);
            ObjectToAttach = null;
        }
    }
}
