using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GPUModel : MonoBehaviour
{
    public GameObject sphere;
    public ComputeShader cs;
    private Simulation simulation = new Simulation();

    void Awake()
    {
        simulation.material = gameObject.GetComponent<MeshRenderer>().material;
        simulation.cs = cs;
        StartCoroutine(simulation.ienumerator(sphere));
    }

    void OnRenderObject()
    {
        simulation.Draw();
    }

    void OnDestroy()
    {
        simulation.Relase();
    }
}

public class Simulation
{
    public Material material;
    public ComputeShader cs;
    private int kernel_Init;
    private int kernel_UpdateSpeed;
    private int kernel_UpdatePosition;


    private static int vertexnum = 32; // 顶点数
    private float L = 4f / (vertexnum - 1); // 顶点间距
    private static float dt = 0.002f;

    private GraphicsBuffer index_buffer; // 顶点索引
    private ComputeBuffer position_buffer; // 顶点位置
    private ComputeBuffer normal_buffer; // 顶点法线
    private ComputeBuffer speed_buffer; // 顶点速度
    private ComputeBuffer force_buffer; // 顶点受力
    private ComputeBuffer uv_buffer; // 顶点uv

    public void Init(GameObject sphere)
    {
        //cs = Resources.Load<ComputeShader>("ClothComputeShader"); // 计算着色器
        Initialize_index_buffer(); // 初始化顶点索引缓冲
        position_buffer = new ComputeBuffer(vertexnum * vertexnum, sizeof(float) * 4); // 初始化顶点位置缓冲
        normal_buffer = new ComputeBuffer(vertexnum * vertexnum, sizeof(float) * 4); // 初始化顶点法线缓冲
        speed_buffer = new ComputeBuffer(vertexnum * vertexnum, sizeof(float) * 4); // 初始化顶点速度缓冲
        force_buffer = new ComputeBuffer(vertexnum * vertexnum, sizeof(float) * 3); // 初始化顶点受力缓冲
        uv_buffer = new ComputeBuffer(vertexnum * vertexnum, sizeof(float) * 2); // 初始化顶点uv缓冲
        kernel_Init = cs.FindKernel("Init"); // 初始化kernel
        kernel_UpdateSpeed = cs.FindKernel("UpdateSpeed"); // 更新速度kernel
        kernel_UpdatePosition = cs.FindKernel("UpdatePosition"); // 更新位置kernel
        //参数设定
        cs.SetInts("vertexnum", vertexnum);
        cs.SetFloat("L", L);
        cs.SetInts("springk", 10000);
        cs.SetInts("mass", 1);
        cs.SetFloat("dt", dt);
        cs.SetFloats("springlength", L, L * Mathf.Sqrt(2), L * 2);
        cs.SetFloats("sphere", sphere.transform.position.x, sphere.transform.position.y, sphere.transform.position.z, sphere.transform.localScale.x / 2);
        cs.SetBuffer(kernel_Init, "position", position_buffer);
        cs.SetBuffer(kernel_Init, "normal", normal_buffer);
        cs.SetBuffer(kernel_Init, "speed", speed_buffer);
        cs.SetBuffer(kernel_Init, "force", force_buffer);
        cs.SetBuffer(kernel_Init, "uv", uv_buffer);
        cs.SetBuffer(kernel_UpdateSpeed, "position", position_buffer);
        cs.SetBuffer(kernel_UpdateSpeed, "normal", normal_buffer);
        cs.SetBuffer(kernel_UpdateSpeed, "speed", speed_buffer);
        cs.SetBuffer(kernel_UpdateSpeed, "force", force_buffer);
        cs.SetBuffer(kernel_UpdatePosition, "position", position_buffer);
        cs.SetBuffer(kernel_UpdatePosition, "normal", normal_buffer);
        cs.SetBuffer(kernel_UpdatePosition, "speed", speed_buffer);
        cs.Dispatch(kernel_Init, vertexnum / 8, vertexnum / 8, 1); // 执行kernel
        material.SetBuffer("position", position_buffer);
        material.SetBuffer("normal", normal_buffer);
        material.SetBuffer("force", force_buffer);
        material.SetBuffer("uv", uv_buffer);
    }

    public void Draw()
    {
        material.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, index_buffer, index_buffer.count, 1);
    }

    // 设置顶点索引
    private void Initialize_index_buffer()
    {
        int trianglenum = (vertexnum - 1) * (vertexnum - 1) * 2;
        index_buffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, trianglenum * 3, sizeof(int)); // 顶点位置
        int[] index = new int[trianglenum * 3];
        for (int i = 0; i < vertexnum - 1; i++)
        {
            for (int j = 0; j < vertexnum - 1; j++)
            {
                int offset = (j * (vertexnum - 1) + i) * 6;
                index[offset + 0] = j * vertexnum + i;
                index[offset + 1] = j * vertexnum + i + 1;
                index[offset + 2] = (j + 1) * vertexnum + i;
                index[offset + 3] = (j + 1) * vertexnum + i;
                index[offset + 4] = j * vertexnum + i + 1;
                index[offset + 5] = (j + 1) * vertexnum + i + 1;
            }
        }
        index_buffer.SetData(new List<int>(index));
    }

    public void Relase()
    {
        index_buffer.Release();
        position_buffer.Release();
        normal_buffer.Release();
        speed_buffer.Release();
        force_buffer.Release();
        uv_buffer.Release();
    }

    public IEnumerator ienumerator(GameObject sphere)
    {
        // 协程初始化
        Init(sphere);
        yield return null;
        float sumt = 0;
        while (true)
        {
            sumt += Time.deltaTime;
            while (sumt > dt)
            {
                cs.SetFloats("sphere", sphere.transform.position.x, sphere.transform.position.y, sphere.transform.position.z, sphere.transform.localScale.x / 2);
                cs.Dispatch(kernel_UpdateSpeed, vertexnum / 8, vertexnum / 8, 1);
                cs.Dispatch(kernel_UpdatePosition, vertexnum / 8, vertexnum / 8, 1);
                sumt -= dt;
            }
            yield return null;
        }
    }
}
