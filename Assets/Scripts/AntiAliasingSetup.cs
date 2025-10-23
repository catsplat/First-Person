using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
// Use URP built-in anti-aliasing types (no PostProcessing v2 dependency)

[ExecuteAlways]
public class AntiAliasingSetup : MonoBehaviour
{
    [Header("Anti-Aliasing Settings")]
    [SerializeField] private AntialiasingMode antiAliasingMode = AntialiasingMode.TemporalAntiAliasing;

    [Header("TAA Settings")]
    [SerializeField, Range(0.1f, 1.0f)] private float taaJitterSpread = 0.75f;
    [SerializeField, Range(0.1f, 1.0f)] private float taaStationaryBlending = 0.95f;
    [SerializeField, Range(0.1f, 1.0f)] private float taaMotionBlending = 0.85f;
    [SerializeField, Range(0.1f, 1.0f)] private float taaSharpness = 0.8f;

    [Header("Post-processing AA Quality")]
    [SerializeField] private AntialiasingQuality smaaQuality = AntialiasingQuality.High;

    private Volume postProcessVolume;
    private UniversalAdditionalCameraData cameraData;

    void Start()
    {
        SetupAntiAliasing();
    }

    void OnValidate()
    {
        SetupAntiAliasing();
    }

    void SetupAntiAliasing()
    {
        // Get or create post process volume
        postProcessVolume = GetComponent<Volume>();
        if (postProcessVolume == null)
        {
            postProcessVolume = gameObject.AddComponent<Volume>();
            postProcessVolume.isGlobal = true;
        }

        // Create profile if needed
        if (postProcessVolume.profile == null)
        {
            postProcessVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        }

        // Setup camera data
        var camera = Camera.main;
        if (camera != null)
        {
            cameraData = camera.GetUniversalAdditionalCameraData();
            if (cameraData != null)
            {
                // Enable post processing
                cameraData.renderPostProcessing = true;

                // Set URP anti-aliasing (MSAA / TAA)
                cameraData.antialiasing = antiAliasingMode;

                if (antiAliasingMode == AntialiasingMode.TemporalAntiAliasing)
                {
                    // Try to set TAA settings via reflection so this compiles across URP versions.
                    // Many URP versions don't expose a public API for setting these fields,
                    // so reflection is the safest cross-version approach.
                    try
                    {
                        var camDataType = cameraData.GetType();
                        var taaProp = camDataType.GetProperty("taaSettings")
                                     ?? camDataType.GetProperty("temporalAASettings")
                                     ?? camDataType.GetProperty("temporalAASettings");

                        if (taaProp != null)
                        {
                            object taaSettings = taaProp.GetValue(cameraData);
                            if (taaSettings != null)
                            {
                                var sType = taaSettings.GetType();

                                // Helper to set a field or property by name if present
                                void SetIfExists(string name, object value)
                                {
                                    var f = sType.GetField(name);
                                    if (f != null)
                                    {
                                        f.SetValue(taaSettings, value);
                                        return;
                                    }
                                    var p = sType.GetProperty(name);
                                    if (p != null && p.CanWrite)
                                    {
                                        p.SetValue(taaSettings, value);
                                    }
                                }

                                SetIfExists("jitterSpread", taaJitterSpread);
                                SetIfExists("stationaryBlending", taaStationaryBlending);
                                SetIfExists("motionBlending", taaMotionBlending);
                                SetIfExists("sharpness", taaSharpness);

                                // Some implementations require setting the modified struct back
                                // onto the camera data via the property setter.
                                if (taaProp.CanWrite)
                                {
                                    taaProp.SetValue(cameraData, taaSettings);
                                }
                                Debug.Log("Applied TAA settings via reflection.");
                            }
                        }
                        else
                        {
                            Debug.Log("TAA settings property not found on UniversalAdditionalCameraData for this URP version.");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning("Failed to apply TAA settings via reflection: " + ex.Message);
                    }
                }
            }
        }

        // Configure URP camera AA quality and log configuration
        if (cameraData != null)
        {
            cameraData.antialiasingQuality = smaaQuality;
        }

        Debug.Log($"Anti-aliasing configured: Mode={antiAliasingMode}, AA Quality={smaaQuality}");
    }
}
