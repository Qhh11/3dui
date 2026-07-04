using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
 
/// <summary>
/// 不依赖 Depth API 的"摄像头前近距离障碍"检测。
/// 直接分析相机图的中心区域(Y 亮度通道)：
///   - 平均亮度太低  → 镜头被捂住(手指/贴面)
///   - 方差(对比度)太低 → 贴到无纹理的近处表面(墙、桌面)
/// 任一命中即视为被挡。需要 ARCameraManager 在出帧(AR 会话运行中)。
/// </summary>
public class CameraBlockCheck : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("AR 相机上的 ARCameraManager；留空自动找")]
    public ARCameraManager cameraManager;
 
    [Header("判定(不依赖 Depth)")]
    [Tooltip("中心平均亮度(0-255)低于此值视为被捂住/太暗")]
    public float darkThreshold = 25f;
    [Tooltip("中心纹理方差低于此值视为贴到无特征近表面(方差≈对比度)")]
    public float flatVarianceThreshold = 40f;
    [Tooltip("中心采样块半径(像素，基于相机图分辨率)")]
    public int samplePatchRadius = 24;
    [Tooltip("拿不到画面数据时是否放行(true=不拦截)")]
    public bool assumeClearIfNoData = true;
 
    private float lastMean = -1f;
    private float lastVar = -1f;
 
    void Awake()
    {
        if (cameraManager == null) cameraManager = FindObjectOfType<ARCameraManager>();
    }
 
    /// <summary>正前方近距离是否被障碍遮挡。</summary>
    public bool IsBlocked()
    {
        lastMean = -1f;
        lastVar = -1f;
 
        if (cameraManager == null)
            return !assumeClearIfNoData;
 
        if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            return !assumeClearIfNoData;
 
        try
        {
            Analyze(image, out lastMean, out lastVar);
        }
        finally
        {
            image.Dispose();
        }
 
        if (lastMean < 0f)                     // 没采到有效像素
            return !assumeClearIfNoData;
 
        return lastMean < darkThreshold || lastVar < flatVarianceThreshold;
    }
 
    private void Analyze(XRCpuImage image, out float mean, out float variance)
    {
        mean = -1f;
        variance = -1f;
 
        XRCpuImage.Plane plane = image.GetPlane(0);   // 0 号平面 = Y 亮度
        NativeArray<byte> data = plane.data;
 
        int width = image.width;
        int height = image.height;
        int rowStride = plane.rowStride;
        int pixelStride = plane.pixelStride;
 
        int cx = width / 2;
        int cy = height / 2;
        int r = Mathf.Max(1, samplePatchRadius);
 
        double sum = 0.0;
        double sumSq = 0.0;
        int n = 0;
 
        for (int y = cy - r; y <= cy + r; y++)
        {
            if (y < 0 || y >= height) continue;
            for (int x = cx - r; x <= cx + r; x++)
            {
                if (x < 0 || x >= width) continue;
 
                int idx = y * rowStride + x * pixelStride;
                if (idx < 0 || idx >= data.Length) continue;
 
                byte v = data[idx];
                sum += v;
                sumSq += (double)v * v;
                n++;
            }
        }
 
        if (n == 0) return;
 
        double m = sum / n;
        mean = (float)m;
        variance = (float)(sumSq / n - m * m);
    }
 
    // 调试用：真机上把这两个打到屏幕，看"正常 / 捂住 / 贴墙"各是多少来定阈值
    public float LastMean => lastMean;
    public float LastVariance => lastVar;
}