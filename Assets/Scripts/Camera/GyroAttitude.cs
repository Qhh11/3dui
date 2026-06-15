using UnityEngine;

/// <summary>
/// 把手机陀螺仪姿态转换到 Unity 坐标系，并按当前屏幕方向(竖屏/横屏)自动校正。
/// 用法：在各控制脚本里把私有的 ReadDeviceAttitude() 内容改成：
///     return GyroAttitude.Read();
/// </summary>
public static class GyroAttitude
{
    public static Quaternion Read()
    {
        Quaternion a = Input.gyro.attitude;
        // 右手系 -> 左手系
        Quaternion q = new Quaternion(a.x, a.y, -a.z, -a.w);

        // 按屏幕方向，给基准校正补一个绕视线轴的 roll
        float roll;
        switch (Screen.orientation)
        {
            case ScreenOrientation.LandscapeLeft:      roll = 90f;  break;
            case ScreenOrientation.LandscapeRight:     roll = -90f; break;
            case ScreenOrientation.PortraitUpsideDown: roll = 180f; break;
            default:                                   roll = 0f;   break; // Portrait
        }

        return Quaternion.Euler(90f, 0f, roll) * q;
    }
}
