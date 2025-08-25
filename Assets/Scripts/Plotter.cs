using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Plotter : MonoBehaviour
{
    [SerializeField] private GameObject dotPrefab;
    [SerializeField] private Material dotMaterial;
    [SerializeField] private float graphSize = 4;
    [SerializeField] private int resolution = 10;
    [SerializeField] private int iterations = 8;
    [SerializeField] private float threshold = 20;
    [SerializeField] private float c = 1.2f;
    [SerializeField] private int batchSize = 30;

    [SerializeField] private int renderBatchSize = 10000;

    private Mesh dotMesh;
    private RenderParams rp;
    private List<Matrix4x4> instData;
    private Matrix4x4[] renderData;
    
    private bool prepared = false;
    
    private void Awake()
    {
        dotMesh = dotPrefab.GetComponent<MeshFilter>().sharedMesh;
        rp = new RenderParams(dotMaterial);
        instData = new List<Matrix4x4>();
        StartCoroutine(CalculateMondelbulb());
    }

    private void Update()
    {
        if (prepared)
        {
            var count = renderData.Length;
            var batchCount = (count + batchSize - 1) / batchSize;
            for (int batch = 0; batch < batchCount; batch++)
            {
                var start = batch * batchSize;
                var length = Math.Min(batchSize, count - start);    
                Graphics.RenderMeshInstanced(rp, dotMesh, 0, renderData, length, start);
            }
        }
    }

    private bool InMondelbulb(float x, float y, float z)
    {
        float r = 0f;
        //print($"Point {x} ,{y}, {z}");
        for (int k = 0; k < iterations; k++)
        {
            r = Mathf.Sqrt(Mathf.Pow(x, 2) + Mathf.Pow(y, 2) + Mathf.Pow(z, 2));
            //print($"Iteration {k} : r - {r}");
            if (r > threshold)
                return false;

            var o = Mathf.Acos(y / r);
            var i = Mathf.Atan2(x, z);

            r = Mathf.Pow(r, 8);
            o *= 8;
            i *= 8;

            x = r * Mathf.Sin(o) * Mathf.Sin(i) + c;
            y = r * Mathf.Cos(o) + c;
            z = r * Mathf.Sin(o) * Mathf.Cos(i) + c;
        }
        if (r > threshold)
            return false;

        return true;
    }

    private IEnumerator CalculateMondelbulb()
    {
        int i = 0;
        int count = 0;
        var dotSize = graphSize / resolution;
        for (float x = (dotSize - graphSize) / 2; x < (graphSize - dotSize) / 2 + Double.Epsilon; x += dotSize)
        for (float y = (dotSize - graphSize) / 2; y < (graphSize - dotSize) / 2 + Double.Epsilon; y += dotSize)
        for (float z = (dotSize - graphSize) / 2; z < (graphSize - dotSize) / 2 + Double.Epsilon; z += dotSize)
        {
            if (InMondelbulb(x, y, z))
            {
                instData.Add(Matrix4x4.Translate(new Vector3(x, y, z)) * Matrix4x4.Scale(new Vector3(dotSize, dotSize, dotSize)));
                i++;
            }

            count++;
            
            if (count % batchSize == 0)
            {
                print(count);
                yield return null;
            }
        }

        prepared = true;
        renderData = instData.ToArray();
    }
}