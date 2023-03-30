using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CPUModel : MonoBehaviour
{
    public enum model { Explicit, PBD_Gauss_Seidel, PBD_Jacobi, PBD_SOR, XPBD, Implicit, Implicit_Chebyshev }
    public model model_type = model.Implicit;
    float t = 0.0333f;
    [Range(0.5f, 10)]
    public float mass = 1;
    [Range(0.9f, 0.99f)]
    public float damping = 0.99f;
    [Range(0, 5f)]
    public float wind = 0f;
    [Range(1000f, 10000f)]
    public float spring_k = 8000;
    [Range(1, 32)]
    public int iteration = 32;
    public enum texture { tex, force, stress }
    public texture texture_type = texture.tex;
    Vector3 gravity = new Vector3(0, -9.8f, 0);
    int n = 21;
    float r = 2.7f;
    int[] E;
    float[] L;
    Vector3[] X;
    Vector3[] V;
    Vector2[] UV_tex;
    float[] lambda;
    bool point_move = false;
    int point_index = -1;
    Material material;

    void Start()
    {
        material = gameObject.GetComponent<MeshRenderer>().material;
        Mesh mesh = GetComponent<MeshFilter>().mesh;

        //Resize the mesh.
        X = new Vector3[n * n];
        UV_tex = new Vector2[n * n];
        int[] triangles = new int[(n - 1) * (n - 1) * 6];
        for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                X[j * n + i] = new Vector3(5 - 10.0f * i / (n - 1), 0, 5 - 10.0f * j / (n - 1));
                UV_tex[j * n + i] = new Vector3(i / (n - 1.0f), j / (n - 1.0f));
            }
        int t = 0;
        for (int j = 0; j < n - 1; j++)
            for (int i = 0; i < n - 1; i++)
            {
                triangles[t * 6 + 0] = j * n + i;
                triangles[t * 6 + 1] = j * n + i + 1;
                triangles[t * 6 + 2] = (j + 1) * n + i + 1;
                triangles[t * 6 + 3] = j * n + i;
                triangles[t * 6 + 4] = (j + 1) * n + i + 1;
                triangles[t * 6 + 5] = (j + 1) * n + i;
                t++;
            }
        mesh.vertices = X;
        mesh.triangles = triangles;
        mesh.uv = UV_tex;
        mesh.RecalculateNormals();


        //Construct the original E
        int[] _E = new int[triangles.Length * 2];
        for (int i = 0; i < triangles.Length; i += 3)
        {
            _E[i * 2 + 0] = triangles[i + 0];
            _E[i * 2 + 1] = triangles[i + 1];
            _E[i * 2 + 2] = triangles[i + 1];
            _E[i * 2 + 3] = triangles[i + 2];
            _E[i * 2 + 4] = triangles[i + 2];
            _E[i * 2 + 5] = triangles[i + 0];
        }
        //Reorder the original edge list
        for (int i = 0; i < _E.Length; i += 2)
            if (_E[i] > _E[i + 1])
                Swap(ref _E[i], ref _E[i + 1]);
        //Sort the original edge list using quicksort
        Quick_Sort(ref _E, 0, _E.Length / 2 - 1);

        int e_number = 0;
        for (int i = 0; i < _E.Length; i += 2)
            if (i == 0 || _E[i + 0] != _E[i - 2] || _E[i + 1] != _E[i - 1])
                e_number++;

        E = new int[e_number * 2];
        for (int i = 0, e = 0; i < _E.Length; i += 2)
            if (i == 0 || _E[i + 0] != _E[i - 2] || _E[i + 1] != _E[i - 1])
            {
                E[e * 2 + 0] = _E[i + 0];
                E[e * 2 + 1] = _E[i + 1];
                e++;
            }

        L = new float[E.Length / 2];
        for (int e = 0; e < E.Length / 2; e++)
        {
            int v0 = E[e * 2 + 0];
            int v1 = E[e * 2 + 1];
            L[e] = (X[v0] - X[v1]).magnitude;
        }

        V = new Vector3[X.Length];
        for (int i = 0; i < V.Length; i++)
            V[i] = new Vector3(0, 0, 0);
    }

    void Quick_Sort(ref int[] a, int l, int r)
    {
        int j;
        if (l < r)
        {
            j = Quick_Sort_Partition(ref a, l, r);
            Quick_Sort(ref a, l, j - 1);
            Quick_Sort(ref a, j + 1, r);
        }
    }

    int Quick_Sort_Partition(ref int[] a, int l, int r)
    {
        int pivot_0, pivot_1, i, j;
        pivot_0 = a[l * 2 + 0];
        pivot_1 = a[l * 2 + 1];
        i = l;
        j = r + 1;
        while (true)
        {
            do ++i; while (i <= r && (a[i * 2] < pivot_0 || a[i * 2] == pivot_0 && a[i * 2 + 1] <= pivot_1));
            do --j; while (a[j * 2] > pivot_0 || a[j * 2] == pivot_0 && a[j * 2 + 1] > pivot_1);
            if (i >= j) break;
            Swap(ref a[i * 2], ref a[j * 2]);
            Swap(ref a[i * 2 + 1], ref a[j * 2 + 1]);
        }
        Swap(ref a[l * 2 + 0], ref a[j * 2 + 0]);
        Swap(ref a[l * 2 + 1], ref a[j * 2 + 1]);
        return j;
    }

    void Swap(ref int a, ref int b)
    {
        int temp = a;
        a = b;
        b = temp;
    }

    void Collision_Handling()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        X = mesh.vertices;

        //Handle colllision.
        GameObject sphere = GameObject.Find("Sphere");
        Vector3 center = sphere.transform.position;
        float d;
        for (int i = 0; i < X.Length; i++)
        {
            if (point_move && i == point_index) continue;
            if (i == 0 || i == 20) continue;
            Vector3 v = X[i] - center;
            d = v.magnitude;
            if (d < r)
            {
                V[i] += 1 / t * (r * v / d - v);
                X[i] = center + r * v / d;
            }
        }
        mesh.vertices = X;
    }

    void Strain_Limiting()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;

        if (model_type == model.PBD_Gauss_Seidel)
        {
            Vector3[] vertices_last = mesh.vertices;
            for (int e = 0; e < E.Length / 2; e++)
            {
                int i = E[e * 2 + 0];
                int j = E[e * 2 + 1];
                Vector3 deltaL = vertices[i] - vertices[j];
                vertices[i] -= 0.5f * (deltaL - L[e] * deltaL / deltaL.magnitude);
                vertices[j] += 0.5f * (deltaL - L[e] * deltaL / deltaL.magnitude);
            }
            for (int i = 0; i < vertices.Length; i++)
            {
                if (point_move && i == point_index) vertices[i] = vertices_last[i];
                if (i == 0 || i == 20) vertices[i] = vertices_last[i];
                V[i] += 1 / t * (vertices[i] - vertices_last[i]);
            }
        }
        else if (model_type == model.PBD_Jacobi || model_type == model.PBD_SOR)
        {
            //Apply PBD here.
            Vector3[] sum_x = new Vector3[vertices.Length];
            int[] sum_n = new int[vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                sum_x[i] = new Vector3(0, 0, 0);
                sum_n[i] = 0;
            }

            for (int e = 0; e < E.Length / 2; e++)
            {
                int i = E[e * 2 + 0];
                int j = E[e * 2 + 1];
                Vector3 deltaL = vertices[i] - vertices[j];
                sum_x[i] += 0.5f * (vertices[i] + vertices[j] + L[e] * deltaL / deltaL.magnitude);
                sum_n[i] += 1;
                sum_x[j] += 0.5f * (vertices[i] + vertices[j] - L[e] * deltaL / deltaL.magnitude);
                sum_n[j] += 1;
            }

            for (int i = 0; i < vertices.Length; i++)
            {
                if (point_move && i == point_index) continue;
                if (i == 0 || i == 20) continue;
                V[i] += 1 / t * ((0.2f * vertices[i] + sum_x[i]) / (0.2f + sum_n[i]) - vertices[i]);
                if (model_type == model.PBD_Jacobi)
                    vertices[i] = (0.2f * vertices[i] + sum_x[i]) / (0.2f + sum_n[i]);
                else
                    vertices[i] = 0.5f * vertices[i] + 0.5f * sum_x[i] / sum_n[i];
            }
        }
        else
        {
            Vector3[] vertices_last = mesh.vertices;

            for (int e = 0; e < E.Length / 2; e++)
            {
                int i = E[e * 2 + 0];
                int j = E[e * 2 + 1];
                Vector3 deltaL = vertices[i] - vertices[j];
                float deltalambda = (deltaL.magnitude - L[e] + spring_k / 8000 * lambda[e]) / (2 + spring_k / 8000);
                lambda[e] += deltalambda;
                vertices[i] -= deltalambda * deltaL / deltaL.magnitude;
                vertices[j] += deltalambda * deltaL / deltaL.magnitude;
            }

            for (int i = 0; i < vertices.Length; i++)
            {
                if (point_move && i == point_index) vertices[i] = vertices_last[i];
                if (i == 0 || i == 20) vertices[i] = vertices_last[i];
                V[i] += 1 / t * (vertices[i] - vertices_last[i]);
            }
        }

        mesh.vertices = vertices;
    }

    void Get_Gradient(Vector3[] X, Vector3[] X_hat, float t, Vector3[] G)
    {
        for (int i = 0; i < X.Length; i++)
        {
            G[i] = 1 / (t * t) * mass * (X[i] - X_hat[i]);
        }
        //Momentum and Gravity.
        for (int i = 0; i < X.Length; i++)
        {
            G[i] -= gravity * mass;
        }
        //Spring Force.
        for (int e = 0; e < E.Length / 2; e++)
        {
            int i = E[e * 2 + 0];
            int j = E[e * 2 + 1];
            Vector3 deltaL = X[i] - X[j];
            if (deltaL.magnitude == 0)
                continue;
            G[i] += spring_k * (1 - L[e] / deltaL.magnitude) * deltaL;
            G[j] -= spring_k * (1 - L[e] / deltaL.magnitude) * deltaL;
        }
    }

    void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        X = mesh.vertices;
        Vector3[] V_last = V.Clone() as Vector3[];

        if (Input.GetMouseButtonDown(0))
        {
            point_move = false;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            for (int i = 0; i < X.Length; i++)
            {
                if (Vector3.Cross(ray.direction, X[i] - ray.origin).magnitude < 0.5f)
                {
                    point_move = true;
                    point_index = i;
                    break;
                }
            }
        }
        if (Input.GetMouseButtonUp(0))
            point_move = false;

        if (point_move)
        {
            Vector3 mouse = Input.mousePosition;
            mouse.z = Camera.main.WorldToScreenPoint(X[point_index]).z;
            X[point_index] = Camera.main.ScreenToWorldPoint(mouse);
            V[point_index] = Vector3.zero;
        }

        if (model_type == model.Explicit)
        {
            for (int i = 0; i < X.Length; i++)
            {
                if (point_move && i == point_index) continue;
                if (i == 0 || i == 20) continue;
                V[i] += t / iteration * gravity;
            }
            for (int k = 0; k < iteration; k++)
            {
                for (int i = 0; i < X.Length; i++)
                {
                    if (point_move && i == point_index) continue;
                    if (i == 0 || i == 20) continue;
                    V[i] += t / iteration * gravity * mass;
                    if (wind > 0)
                        V[i] += wind * (Vector3.Dot(mesh.normals[i], new Vector3(0, 0, wind) - V[i])) * mesh.normals[i] * t / iteration;
                }
                for (int e = 0; e < E.Length / 2; e++)
                {
                    int i = E[e * 2 + 0];
                    int j = E[e * 2 + 1];
                    Vector3 deltaL = X[i] - X[j];
                    if (deltaL.magnitude == 0)
                        continue;
                    V[i] -= t / iteration * spring_k * (1 - L[e] / deltaL.magnitude) * deltaL;
                    V[j] += t / iteration * spring_k * (1 - L[e] / deltaL.magnitude) * deltaL;
                }
                for (int i = 0; i < X.Length; i++)
                {
                    if (point_move && i == point_index) continue;
                    if (i == 0 || i == 20) continue;
                    X[i] += t / iteration * V[i];
                }
            }
            mesh.vertices = X;
        }
        else if (model_type == model.PBD_Gauss_Seidel || model_type == model.PBD_Jacobi || model_type == model.XPBD || model_type == model.PBD_SOR)
        {
            if (model_type == model.XPBD)
            {
                lambda = new float[E.Length / 2];
            }
            for (int i = 0; i < X.Length; i++)
            {
                if (point_move && i == point_index) continue;
                if (i == 0 || i == 20) continue;
                V[i] = V[i] * damping;
                V[i] += t * gravity * mass;
                if (wind > 0)
                    V[i] += wind * (Vector3.Dot(mesh.normals[i], new Vector3(0, 0, wind) - V[i])) * mesh.normals[i] * t;
                X[i] += t * V[i];
            }
            mesh.vertices = X;

            for (int k = 0; k < iteration; k++)
                Strain_Limiting();
        }
        else if (model_type == model.Implicit)
        {
            Vector3[] X_hat = new Vector3[X.Length];
            Vector3[] G = new Vector3[X.Length];

            for (int i = 0; i < V.Length; i++)
            {
                if (point_move && i == point_index) continue;
                if (i == 0 || i == 20) continue;
                V[i] *= damping;
                if (wind > 0)
                    V[i] += wind * (Vector3.Dot(mesh.normals[i], new Vector3(0, 0, wind) - V[i])) * mesh.normals[i] * t;
            }
            for (int i = 0; i < X.Length; i++)
            {
                X_hat[i] = X[i] + t * V[i];
                X[i] = X_hat[i];
            }

            float d = 1 / (1 / (t * t) * mass + 4 * spring_k);
            for (int k = 0; k < iteration; k++)
            {
                Get_Gradient(X, X_hat, t, G);
                for (int i = 0; i < X.Length; i++)
                {
                    if (point_move && i == point_index) continue;
                    if (i == 0 || i == 20) continue;
                    X[i] -= d * G[i];
                }
            }

            for (int i = 0; i < X.Length; i++)
            {
                if (point_move && i == point_index) continue;
                if (i == 0 || i == 20) continue;
                V[i] += 1 / t * (X[i] - X_hat[i]);
            }

            mesh.vertices = X;
        }
        else if (model_type == model.Implicit_Chebyshev)
        {
            Vector3[] X_hat = new Vector3[X.Length];
            Vector3[] G = new Vector3[X.Length];
            Vector3[] last_X = new Vector3[X.Length];
            float w = 1;
            float rho = 0.7f;

            for (int i = 0; i < V.Length; i++)
            {
                if (point_move && i == point_index) continue;
                if (i == 0 || i == 20) continue;
                V[i] *= damping;
                if (wind > 0)
                    V[i] += wind * (Vector3.Dot(mesh.normals[i], new Vector3(0, 0, wind) - V[i])) * mesh.normals[i] * t;
            }
            for (int i = 0; i < X.Length; i++)
            {
                X_hat[i] = X[i] + t * V[i];
                X[i] = X_hat[i];
                last_X[i] = new Vector3(0, 0, 0);
            }

            for (int k = 0; k < iteration; k++)
            {
                if (k == 0)
                {
                    w = 1;
                }
                else if (k == 1)
                {
                    w = 2 / (2 - rho * rho);
                }
                else
                {
                    w = 4 / (4 - rho * rho * w);
                }
                Get_Gradient(X, X_hat, t, G);
                for (int i = 0; i < X.Length; i++)
                {
                    if (point_move && i == point_index) continue;
                    if (i == 0 || i == 20) continue;
                    Vector3 old_X = X[i];
                    X[i] -= 1 / (1 / (t * t) * mass + 4 * spring_k) * G[i];
                    X[i] = w * X[i] + (1 - w) * last_X[i];
                    last_X[i] = old_X;
                }
            }

            for (int i = 0; i < X.Length; i++)
            {
                if (point_move && i == point_index) continue;
                if (i == 0 || i == 20) continue;
                V[i] += 1 / t * (X[i] - X_hat[i]);
            }

            mesh.vertices = X;
        }

        Collision_Handling();

        if (texture_type == texture.tex)
        {
            mesh.uv = UV_tex;
            material.SetFloat("_Texture", 0);
        }
        else if (texture_type == texture.stress && (model_type == model.Implicit || model_type == model.Implicit_Chebyshev))
        {
            Vector2[] UV = new Vector2[X.Length];
            Vector3[] stress = new Vector3[X.Length];
            for (int e = 0; e < E.Length / 2; e++)
            {
                int i = E[e * 2 + 0];
                int j = E[e * 2 + 1];
                Vector3 deltaL = X[i] - X[j];
                if (deltaL.magnitude == 0)
                    continue;
                stress[i] += spring_k * (1 - L[e] / deltaL.magnitude) * deltaL;
                stress[j] += spring_k * (1 - L[e] / deltaL.magnitude) * deltaL;
            }
            for (int i = 0; i < X.Length; i++)
            {
                UV[i].x = stress[i].magnitude;
            }
            mesh.uv = UV;
            material.SetFloat("_Texture", 2);
        }
        else
        {
            Vector2[] UV = new Vector2[X.Length];
            for (int i = 0; i < X.Length; i++)
            {
                UV[i].x = (V[i] - V_last[i]).magnitude;
            }
            mesh.uv = UV;
            material.SetFloat("_Texture", 1);
        }

        mesh.RecalculateNormals();
    }

    public void setmass(float _mass)
    {
        mass = _mass;
    }
    public void setdamping(float _damping)
    {
        damping = _damping;
    }
    public void setwind(float _wind)
    {
        wind = _wind;
    }
    public void setspring_k(float _spring_k)
    {
        spring_k = _spring_k;
    }
    public void setiteration(float _iteration)
    {
        iteration = (int)_iteration;
    }
    public void setmodel(int _model)
    {
        model_type = (model)_model;
        if (model_type == model.XPBD)
            GameObject.Find("Slider (4)").GetComponent<Slider>().value = 1;
        else
            GameObject.Find("Slider (4)").GetComponent<Slider>().value = 32;
    }
    public void settexture(int _texture)
    {
        texture_type = (texture)_texture;
    }
    public void restart()
    {
        Start();
    }
}
