using UnityEngine;

/// <summary>
/// Provides means to deep-copy a TerrainData object because Unitys' built-in "Instantiate" method
/// will miss some things and the resulting copy still shares data with the original.
/// Forked and modified by pilarski@ualberta.ca from:
/// https://gist.github.com/Alan-Baylis/e8ed254d83579cd119f2c5efe33b6d9e
/// </summary>
public class TerrainDataCloner
{
    /// <summary>
    /// Creates a real deep-copy of a TerrainData
    /// </summary>
    /// <param name="template">TerrainData to duplicate</param>
    /// <returns>New terrain data instance</returns>
    public static TerrainData Clone(TerrainData template, TerrainData baseTerrain)
    {

        TerrainData dup = null;
        if (baseTerrain == null)
            dup = new TerrainData();
        else
        {
            dup = baseTerrain;
        }

        dup.alphamapResolution = template.alphamapResolution;
        dup.baseMapResolution = template.baseMapResolution;
        dup.detailPrototypes = CloneDetailPrototypes(template.detailPrototypes);
        dup.enableHolesTextureCompression = template.enableHolesTextureCompression;
        dup.heightmapResolution = template.heightmapResolution;
        dup.size = template.size;
        dup.terrainLayers = template.terrainLayers;
        dup.wavingGrassAmount = template.wavingGrassAmount;
        dup.wavingGrassSpeed = template.wavingGrassSpeed;
        dup.wavingGrassStrength = template.wavingGrassStrength;
        dup.wavingGrassTint = template.wavingGrassTint;

        dup.SetAlphamaps(0, 0, template.GetAlphamaps(0, 0, template.alphamapWidth, template.alphamapHeight));
        dup.SetDetailResolution(template.detailResolution, template.detailResolutionPerPatch);
        for (int n = 0; n < template.detailPrototypes.Length; n++)
        {
            dup.SetDetailLayer(0, 0, n, template.GetDetailLayer(0, 0, template.detailWidth, template.detailHeight, n));
        }
        dup.SetHeights(0, 0, template.GetHeights(0, 0, template.heightmapResolution, template.heightmapResolution));
        dup.SetHoles(0, 0, template.GetHoles(0, 0, template.holesResolution, template.holesResolution));

        dup.treePrototypes = CloneTreePrototypes(template.treePrototypes);
        dup.treeInstances = CloneTreeInstances(template.treeInstances);
        return dup;
    }

    /// <summary>
    /// Deep-copies an array of detail prototype instances
    /// </summary>
    /// <param name="original">Prototypes to clone</param>
    /// <returns>Cloned array</returns>
    static DetailPrototype[] CloneDetailPrototypes(DetailPrototype[] original)
    {
        DetailPrototype[] protoDuplicate = new DetailPrototype[original.Length];

        for (int n = 0; n < original.Length; n++)
        {
            protoDuplicate[n] = new DetailPrototype
            {
                dryColor = original[n].dryColor,
                healthyColor = original[n].healthyColor,
                holeEdgePadding = original[n].holeEdgePadding,
                maxHeight = original[n].maxHeight,
                maxWidth = original[n].maxWidth,
                minHeight = original[n].minHeight,
                minWidth = original[n].minWidth,
                noiseSpread = original[n].noiseSpread,
                prototype = original[n].prototype,
                prototypeTexture = original[n].prototypeTexture,
                renderMode = original[n].renderMode,
                usePrototypeMesh = original[n].usePrototypeMesh
            };
        }
        return protoDuplicate;
    }

    /// <summary>
    /// Deep-copies an array of tree prototype instances
    /// </summary>
    /// <param name="original">Prototypes to clone</param>
    /// <returns>Cloned array</returns>
    static TreePrototype[] CloneTreePrototypes(TreePrototype[] original)
    {
        TreePrototype[] protoDuplicate = new TreePrototype[original.Length];

        for (int n = 0; n < original.Length; n++)
        {
            protoDuplicate[n] = new TreePrototype
            {
                bendFactor = original[n].bendFactor,
                prefab = original[n].prefab,
            };
        }
        return protoDuplicate;
    }

    /// <summary>
    /// Deep-copies an array of tree instances
    /// </summary>
    /// <param name="original">Trees to clone</param>
    /// <returns>Cloned array</returns>
    static TreeInstance[] CloneTreeInstances(TreeInstance[] original)
    {
        TreeInstance[] treeInst = new TreeInstance[original.Length];

        System.Array.Copy(original, treeInst, original.Length);
        return treeInst;
    }
}