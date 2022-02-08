using UnityEngine;

public class TargetFPS : MonoBehaviour
{
    public int TargetFramerate = 30;

    void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = TargetFramerate;
    }

    void Update()
    {
        if (Application.targetFrameRate != TargetFramerate)
            Application.targetFrameRate = TargetFramerate;
    }
}
