using UnityEngine;

// 关键特性：让脚本在编辑模式下也能运行，无需点击Play
[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class EditorTriangle : MonoBehaviour
{
    // 当物体加载或参数变化时自动执行
    void OnEnable()
    {
        CreateTriangleMesh();
    }

    void CreateTriangleMesh()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter.sharedMesh == null)
        {
            Mesh mesh = new Mesh();
            mesh.name = "EditorTriangle";

            // 顶点坐标（形成一个底边1单位，高1单位的三角形）
            Vector3[] vertices = new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(0, 1, 0)
            };

            // 三角形索引（逆时针顺序保证正面可见）
            int[] triangles = new int[] { 0, 2, 1 };

            // UV坐标（如果后续要贴图片，这行必须有）
            Vector2[] uv = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1)
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;

            // 计算法线，否则光照会异常
            mesh.RecalculateNormals();
            meshFilter.sharedMesh = mesh;
        }

        // 给一个默认颜色，防止出现粉色丢失材质
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer.sharedMaterial == null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = Color.cyan;
            renderer.sharedMaterial = mat;
        }
    }
}