using UnityEngine;

using TMPro;

public class FPS : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text fpsText;

    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.25f;

    private float timer;
    private int frameCount;

    public void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
    }

    void Update()
    {
        frameCount++;
        timer += Time.unscaledDeltaTime;

        if (timer >= updateInterval)
        {
            float fps = frameCount / timer;
            fpsText.text = Mathf.RoundToInt(fps) + " FPS";

            frameCount = 0;
            timer = 0f;
        }
    }
}
