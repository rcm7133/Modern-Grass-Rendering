using UnityEngine;

public class GrassRenderer : MonoBehaviour
{
    public Material grassMaterialHLOD;
    public Material grassMaterialLLOD;
    public Transform cameraTransform;
    
    // Compute Buffer
    public ComputeBuffer grassBuffer; // from GrassInit
    public ComputeBuffer grassBufferHLOD;
    public ComputeBuffer grassBufferLLOD;
    public ComputeBuffer grassBufferCulled;
    private ComputeBuffer argsBufferHLOD;
    private ComputeBuffer argsBufferLLOD;
    
    [Header("LOD Settings")]
    public float lodDistance;
    public Mesh HLODMesh;
    public Mesh LLODMesh;
    [Header("Culling Settings")]
    public bool frustumCulling = true;
    [Header("Sway Settings")]
    public float swayAmplitude;
    public float swayFrequency;
    public float swayNoiseStrength;
    public float swayScrollSpeed;
    public RenderTexture swayTexture;
    public int swayTextureWidth;
    public Texture2D noise;
    
    public GrassInit grassInit;
    [Header("Compute Shaders")]
    public ComputeShader grassSwayCompute;
    public ComputeShader lodCompute;
    
    void Start()
    {
        
        if (grassInit.grassBuffer == null)
            grassInit.GenerateGrass();
        
        grassBuffer = grassInit.grassBuffer;
        grassInit.grassMaterial.SetTexture("_GrassSwayTexture",  swayTexture);

        int stride = sizeof(float) * 7;
        
        grassBufferHLOD = new ComputeBuffer(grassInit.grassCount, stride, ComputeBufferType.Append);
        grassBufferLLOD = new ComputeBuffer(grassInit.grassCount, stride, ComputeBufferType.Append);
        grassBufferCulled = new ComputeBuffer(grassInit.grassCount, stride, ComputeBufferType.Append);
        
        // args: index count, instance count, start index, base vertex, start instance
        // High LOD Args
        uint[] argsHLOD = new uint[5] { HLODMesh.GetIndexCount(0), 0, HLODMesh.GetIndexStart(0), HLODMesh.GetBaseVertex(0), 0 };
        argsBufferHLOD = new ComputeBuffer(1, argsHLOD.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBufferHLOD.SetData(argsHLOD);
        // High LOD Args
        uint[] argsLLOD = new uint[5] { LLODMesh.GetIndexCount(0), 0, LLODMesh.GetIndexStart(0), LLODMesh.GetBaseVertex(0), 0 };
        argsBufferLLOD = new ComputeBuffer(1, argsLLOD.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBufferLLOD.SetData(argsLLOD);
        
    }

    void Update()
    {
        if (grassBuffer == null)
            return;
        
        grassMaterialHLOD.SetBuffer("_GrassData", grassBuffer);
        grassMaterialHLOD.SetFloat("_GrassHeight", grassInit.grassHeight);
        grassMaterialHLOD.SetVector("_CameraPos", cameraTransform.position);

        grassMaterialLLOD.SetBuffer("_GrassData", grassBuffer);
        grassMaterialLLOD.SetFloat("_GrassHeight", grassInit.grassHeight);
        grassMaterialLLOD.SetVector("_CameraPos", cameraTransform.position);
        // Sway Compute
        int kernel = grassSwayCompute.FindKernel("CSMain");
        
        grassSwayCompute.SetFloat("amplitude", swayAmplitude);
        grassSwayCompute.SetFloat("frequency", swayFrequency);
        grassSwayCompute.SetFloat("noiseStrength", swayNoiseStrength);
        grassSwayCompute.SetFloat("time", Time.time);
        grassSwayCompute.SetFloat("speed", swayScrollSpeed);
        grassSwayCompute.SetTexture(kernel, "noise", noise);
        grassSwayCompute.SetTexture(kernel, "Result", swayTexture);
        
        int groupX = Mathf.CeilToInt(swayTextureWidth / 8f);
        int groupZ = Mathf.CeilToInt(swayTextureWidth / 8f);
        grassSwayCompute.Dispatch(kernel, groupX, groupZ, 1);
        
        // Frustum Culling Planes \\
        // Camera clipping planes for GPU frustum culling
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        Vector4[] frustumPlanes = new  Vector4[6];

        for (int i = 0; i < 6; i++)
        {
            frustumPlanes[i] = new Vector4(
                planes[i].normal.x,
                planes[i].normal.y,
                planes[i].normal.z,
                planes[i].distance
            );
        }
        
        // LOD Compute
        kernel = lodCompute.FindKernel("CSMain");
        
        lodCompute.SetBool("frustumCulling", frustumCulling);
        lodCompute.SetVectorArray("frustumPlanes", frustumPlanes);
        lodCompute.SetBuffer(kernel, "grassData", grassBuffer);
        lodCompute.SetBuffer(kernel, "grassDataHLOD", grassBufferHLOD);
        lodCompute.SetBuffer(kernel, "grassDataLLOD", grassBufferLLOD);
        lodCompute.SetBuffer(kernel, "culledGrassData", grassBufferCulled);

        lodCompute.SetVector("cameraPosition", cameraTransform.position);
        lodCompute.SetFloat("lodDistance", lodDistance);
        lodCompute.SetInt("grassCount", grassInit.grassCount);

        grassBufferHLOD.SetCounterValue(0);
        grassBufferLLOD.SetCounterValue(0);
        grassBufferCulled.SetCounterValue(0);

        int groups = Mathf.CeilToInt(grassInit.grassCount / 64f);
        lodCompute.Dispatch(kernel, groups, 1, 1);
        
        Bounds bounds = new Bounds(
            cameraTransform.position,
            Vector3.one * lodDistance * 2f
        );
        
        ComputeBuffer.CopyCount(grassBufferHLOD, argsBufferHLOD, sizeof(uint));
        ComputeBuffer.CopyCount(grassBufferLLOD, argsBufferLLOD, sizeof(uint));
        
        grassMaterialHLOD.SetBuffer("_GrassData", grassBufferHLOD);
        Graphics.DrawMeshInstancedIndirect(HLODMesh, 0, grassMaterialHLOD, bounds, argsBufferHLOD);

        grassMaterialLLOD.SetBuffer("_GrassData", grassBufferLLOD);
        Graphics.DrawMeshInstancedIndirect(LLODMesh, 0, grassMaterialLLOD, bounds, argsBufferLLOD);
    }

    private void OnDestroy()
    {
        if (argsBufferHLOD != null) argsBufferHLOD.Release();
        if (argsBufferLLOD != null) argsBufferLLOD.Release();
        if (grassBufferHLOD != null) grassBufferHLOD.Release();
        if (grassBufferLLOD != null) grassBufferLLOD.Release();
    }
}
