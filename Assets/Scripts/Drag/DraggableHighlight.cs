using UnityEngine;

/// <summary>
/// 挂在可高亮物体上：被点亮时在物体周围画一个发亮的包围盒(亮框)。
/// 一般由 DraggableHighlighter 调用 SetColor / SetHighlighted。
/// 物体最好有 MeshFilter 或 BoxCollider，框才贴合。
/// </summary>
[DisallowMultipleComponent]
public class DraggableHighlight : MonoBehaviour
{
    [Tooltip("框的颜色(可被管理器按 Layer 覆盖)")]
    public Color color = new Color(1f, 0.9f, 0.2f);
    [Tooltip("线宽")]
    public float lineWidth = 3f;
    [Tooltip("框比物体大出来的余量")]
    public float padding = 0f;

    private LineRenderer lr;
    private bool highlighted;

    /// <summary>设置颜色；已经建好框时会立即更新</summary>
    public void SetColor(Color c)
    {
        color = c;
        if (lr != null)
        {
            lr.startColor = lr.endColor = c;
            if (lr.material != null) lr.material.color = c;
        }
    }

    public void SetHighlighted(bool value)
    {
        if (value == highlighted) return;
        highlighted = value;
        if (highlighted && lr == null) Build();
        if (lr != null) lr.enabled = highlighted;
    }

    private void Build()
    {
        var go = new GameObject("HighlightBox");
        go.transform.SetParent(transform, false);

        lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = false;
        lr.widthMultiplier = lineWidth;
        lr.numCornerVertices = 0;
        lr.numCapVertices = 0;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        // 无光照材质，看起来"亮"。URP 下若显示不对，换成 URP 的 Unlit 材质即可
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        var mat = new Material(sh) { color = color };
        lr.material = mat;
        lr.startColor = lr.endColor = color;

        Bounds b = GetLocalBounds();
        Vector3 c = b.center;
        Vector3 e = b.extents + Vector3.one * padding;
        lr.positionCount = 16;
        lr.SetPositions(BoxPath(c, e));
    }

    private Bounds GetLocalBounds()
    {
        var mf = GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null) return mf.sharedMesh.bounds;
        if (TryGetComponent(out BoxCollider bc)) return new Bounds(bc.center, bc.size);

        var col = GetComponent<Collider>();
        if (col != null)
        {
            Vector3 ls = transform.lossyScale;
            Vector3 size = col.bounds.size;
            size = new Vector3(size.x / Mathf.Max(ls.x, 1e-4f),
                               size.y / Mathf.Max(ls.y, 1e-4f),
                               size.z / Mathf.Max(ls.z, 1e-4f));
            return new Bounds(transform.InverseTransformPoint(col.bounds.center), size);
        }
        return new Bounds(Vector3.zero, Vector3.one);
    }

    // 一条折线走完包围盒 12 条边(含少量重走)，每段都是真实的边、无斜线
    private static Vector3[] BoxPath(Vector3 c, Vector3 e)
    {
        Vector3[] k =
        {
            c + new Vector3(-e.x, -e.y, -e.z), // 0
            c + new Vector3( e.x, -e.y, -e.z), // 1
            c + new Vector3( e.x, -e.y,  e.z), // 2
            c + new Vector3(-e.x, -e.y,  e.z), // 3
            c + new Vector3(-e.x,  e.y, -e.z), // 4
            c + new Vector3( e.x,  e.y, -e.z), // 5
            c + new Vector3( e.x,  e.y,  e.z), // 6
            c + new Vector3(-e.x,  e.y,  e.z), // 7
        };
        int[] order = { 0, 1, 2, 3, 0, 4, 5, 1, 5, 6, 2, 6, 7, 3, 7, 4 };
        Vector3[] p = new Vector3[order.Length];
        for (int i = 0; i < order.Length; i++) p[i] = k[order[i]];
        return p;
    }

    void OnDisable()
    {
        if (lr != null) lr.enabled = false;
        highlighted = false;
    }
}
