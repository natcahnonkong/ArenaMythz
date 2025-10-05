using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Time Settings")]
    [Range(0f, 24f)] public float timeOfDay = 12f; // เวลาในเกม (0 = เที่ยงคืน, 12 = เที่ยงวัน)
    public float dayLengthInMinutes = 1f; // ความยาวของ 1 วันในเกม (นาที)

    [Header("Light Settings")]
    public Light sun;
    public Light moon;
    public float sunMaxIntensity = 1f;
    public float moonMaxIntensity = 0.5f;

    [Header("Skybox Settings")]
    public Material daySkybox;
    public Material sunsetSkybox;
    public Material nightSkybox;

    private void Update()
    {
        // ⏰ เดินเวลา
        timeOfDay += (24f / (dayLengthInMinutes * 60f)) * Time.deltaTime;
        if (timeOfDay >= 24f) timeOfDay -= 24f;

        // ☀️🌙 หมุน Sun & Moon
        float sunAngle = (timeOfDay / 24f) * 360f - 90f;
        float moonAngle = sunAngle + 180f;

        sun.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0);
        moon.transform.rotation = Quaternion.Euler(moonAngle, 170f, 0);

        // 🔆 ปรับความสว่างตามความสูง
        float sunDot = Mathf.Clamp01(Vector3.Dot(sun.transform.forward, Vector3.down));
        float moonDot = Mathf.Clamp01(Vector3.Dot(moon.transform.forward, Vector3.down));

        sun.intensity = sunDot * sunMaxIntensity;
        moon.intensity = moonDot * moonMaxIntensity;

        // 🌌 เลือก Skybox ตามเวลา
        Material targetSkybox;
        if (timeOfDay >= 8f && timeOfDay < 16f) // Day
        {
            targetSkybox = daySkybox;
        }
        else if ((timeOfDay >= 6f && timeOfDay < 8f) || (timeOfDay >= 16f && timeOfDay < 16.8f)) // Sunrise / Sunset
        {
            targetSkybox = sunsetSkybox;
        }
        else // Night
        {
            targetSkybox = nightSkybox;
        }

        if (RenderSettings.skybox != targetSkybox)
        {
            RenderSettings.skybox = targetSkybox;
            DynamicGI.UpdateEnvironment();
        }
    }
}
