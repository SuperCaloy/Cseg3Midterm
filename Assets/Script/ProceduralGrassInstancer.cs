using UnityEngine;
using System.Collections.Generic;

public class ProceduralGrassInstancer : MonoBehaviour
{
    public Mesh grassMesh;
    public Material grassMaterial;

    private List<Matrix4x4[]> matrixChunks = new List<Matrix4x4[]>();
    private MaterialPropertyBlock propBlock;

    public void Initialize(List<Matrix4x4> matrices)
    {
        if (grassMesh == null || grassMaterial == null || matrices == null || matrices.Count == 0) return;

        propBlock = new MaterialPropertyBlock();

        // Break matrices into chunks of 1023 (max for DrawMeshInstanced)
        int maxPerBatch = 1023;
        for (int i = 0; i < matrices.Count; i += maxPerBatch)
        {
            int chunkSize = Mathf.Min(maxPerBatch, matrices.Count - i);
            Matrix4x4[] chunk = new Matrix4x4[chunkSize];
            for (int j = 0; j < chunkSize; j++)
            {
                chunk[j] = matrices[i + j];
            }
            matrixChunks.Add(chunk);
        }
    }

    void Update()
    {
        if (grassMesh == null || grassMaterial == null || matrixChunks.Count == 0) return;

        // Draw each chunk
        foreach (var chunk in matrixChunks)
        {
            Graphics.DrawMeshInstanced(grassMesh, 0, grassMaterial, chunk, chunk.Length, propBlock, UnityEngine.Rendering.ShadowCastingMode.Off);
        }
    }
}
