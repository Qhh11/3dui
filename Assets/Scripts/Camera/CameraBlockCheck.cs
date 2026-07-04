using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// 用 AR 环境深度判断"摄像头正前方近距离内是否有障碍物"。
/// 取深度图中心一小块的最近深度，小于 obstacleDistance(米) 即视为被挡。
/// 需要 AROcclusionManager + 设备支持 ARCore Depth；AR 会话要在跑并稍微移动过，深度才有值。
/// </summary>
public class CameraBlockCheck : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("AR 相机上的 AROcclusionManager；留空自动找")]
    public AROcclusionManager occlusionManager;

    [Header("判定")]
    [Tooltip("正前方这个距离(米)内有障碍就算被挡")]
    public float obstacleDistance = 0.3f;
    [Tooltip("中心采样块的半径(深度图像素)，越大越稳但略慢")]
    public int samplePatchRadius = 8;
    [Tooltip("拿不到深度数据时是否放行(true=不拦截，避免误挡)")]
    public bool assumeClearIfNoData = true;

    private float lastMinDepth = -1f;
    private readonly byte[] f4 = new byte[4];

    void Awake()
    {
        if (occlusionManager == null) occlusionManager = FindObjectOfType<AROcclusionManager>();
    }

    void OnEnable()
    {
        if (occlusionManager != null)
            occlusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Fastest;
    }

    /// <summary>正前方近距离内是否有障碍。</summary>
    public bool IsBlocked()
    {
        lastMinDepth = -1f;

        if (occlusionManager == null)
            return !assumeClearIfNoData;

        if (!occlusionManager.TryAcquireEnvironmentDepthCpuImage(out XRCpuImage image))
            return !assumeClearIfNoData;

        try
        {
            lastMinDepth = SampleCenterMinDepth(image);
        }
        finally
        {
            image.Dispose();
        }

        if (lastMinDepth <= 0f)
            return !assumeClearIfNoData;   // 中心没有有效深度

        return lastMinDepth < obstacleDistance;
    }

    private float SampleCenterMinDepth(XRCpuImage image)
    {
        XRCpuImage.Plane plane = image.GetPlane(0);
        NativeArray<byte> data = plane.data;

        int width = image.width;
        int height = image.height;
        int rowStride = plane.rowStride;
        int pixelStride = plane.pixelStride;

        int cx = width / 2;
        int cy = height / 2;
        int r = Mathf.Max(1, samplePatchRadius);

        float min = float.MaxValue;
        bool any = false;

        for (int y = cy - r; y <= cy + r; y++)
        {
            if (y < 0 || y >= height) continue;
            for (int x = cx - r; x <= cx + r; x++)
            {
                if (x < 0 || x >= width) continue;

                int idx = y * rowStride + x * pixelStride;
                float depth = ReadDepth(data, idx, image.format);

                if (depth > 0f && depth < min)
                {
                    min = depth;
                    any = true;
                }
            }
        }

        return any ? min : -1f;
    }

    private float ReadDepth(NativeArray<byte> data, int i, XRCpuImage.Format format)
    {
        switch (format)
        {
            case XRCpuImage.Format.DepthFloat32:
                if (i + 3 >= data.Length) return 0f;
                f4[0] = data[i]; f4[1] = data[i + 1]; f4[2] = data[i + 2]; f4[3] = data[i + 3];
                return BitConverter.ToSingle(f4, 0);                 // 米

            case XRCpuImage.Format.DepthUint16:
                if (i + 1 >= data.Length) return 0f;
                ushort mm = (ushort)(data[i] | (data[i + 1] << 8));
                return mm * 0.001f;                                   // 毫米 -> 米

            default:
                return 0f;
        }
    }

    /// <summary>调试用：上次检测到的最近深度(米)，无数据为 -1。</summary>
    public float LastMinDepth => lastMinDepth;
}