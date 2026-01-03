using UnityEngine;

public class GrassInit : MonoBehaviour
{
    [Header("Terrain Settings")]
    public Transform terrainTransform;
    public Material grassMaterial;
    public float terrainHeight = 10f;

    [Header("Texture Settings")]
    public Texture2D heightMap;
    public Texture2D noise;

    [Header("Grass Settings")]
    public float grassDensity = 1f;
    public float grassHeight = 1f;
    public float grassPosNoiseStrength = 1f;
    public float grassHeightNoiseStrength = 1f;
    public float grassCullCheckRadius = 1f;
    

    [Header("Compute Shader")]
    public ComputeShader grassCompute;

    public ComputeBuffer grassBuffer;

    public int grassCount;

    public void Start()
    {
        GenerateGrass();
    }

    public void GenerateGrass()
    {
        // calculate grid size based on density
        float terrainWidth = terrainTransform.localScale.x;
        float terrainLength = terrainTransform.localScale.z;

        int countX = Mathf.CeilToInt(terrainWidth * grassDensity);
        int countZ = Mathf.CeilToInt(terrainLength * grassDensity);
        grassCount = countX * countZ;

        // Allocate buffer
        grassBuffer = new ComputeBuffer(grassCount, sizeof(float) * 7);
        
        int kernel = grassCompute.FindKernel("CSMain");

        grassCompute.SetVector("terrainPos", terrainTransform.position);
        Vector3 meshSize = terrainTransform.GetComponent<MeshRenderer>().bounds.size;
        grassCompute.SetVector("terrainSize", new Vector3(meshSize.x, terrainHeight, meshSize.z));
        grassCompute.SetInts("gridSize", countX, countZ);
        grassCompute.SetFloat("grassHeight", grassHeight);
        grassCompute.SetFloat("grassCullCheckRadius", grassCullCheckRadius);
        grassCompute.SetTexture(kernel, "heightMap", heightMap);
        grassCompute.SetBuffer(kernel, "grassData", grassBuffer);
        grassCompute.SetFloat("grassPosNoiseStrength", grassPosNoiseStrength);
        grassCompute.SetFloat("grassHeightNoiseStrength", grassHeightNoiseStrength);
        grassCompute.SetTexture(kernel, "noise", noise);
        
        grassMaterial.SetFloat("_MaxGrassHeight",  grassHeight + grassHeightNoiseStrength);
        
        int groupX = Mathf.CeilToInt(countX / 8f);
        int groupZ = Mathf.CeilToInt(countZ / 8f);
        grassCompute.Dispatch(kernel, groupX, groupZ, 1);
    }
    

    private void OnDestroy()
    {
        if (grassBuffer != null)
            grassBuffer.Release();
    }
}