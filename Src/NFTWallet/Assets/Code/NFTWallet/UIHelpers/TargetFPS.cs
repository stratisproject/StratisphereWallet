using UnityEngine;

public class TargetFPS : MonoBehaviour
{
    public int TargetFramerate = 30;

    void Awake()
    {
        #if !UNITY_ANDROID && !UNITY_IPHONE
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = TargetFramerate;
        #endif
    }

    void Update()
    {
        #if !UNITY_ANDROID && !UNITY_IPHONE
                if (Application.targetFrameRate != TargetFramerate)
                    Application.targetFrameRate = TargetFramerate;
        #endif
    }
}
