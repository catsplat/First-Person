using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Volume))]
public class EyeAdaptation : MonoBehaviour
{
    [Header("Required References")]
    [Tooltip("Assign the LuminanceCompute.compute shader here")]
    [SerializeField] private ComputeShader computeShader;

    [Header("Adaptation Settings")]
    [Tooltip("How quickly the camera adjusts to brighter areas (higher = faster)")]
    [Range(0.1f, 10.0f)]
    public float adaptationSpeedToBright = 3.0f;
    
    [Tooltip("How quickly the camera adjusts to darker areas (higher = faster)")]
    [Range(0.1f, 10.0f)]
    public float adaptationSpeedToDark = 2.0f;
    
    [Tooltip("Target middle-gray value for metering (lower = darker image)")]
    [Range(0.1f, 1.0f)]
    public float targetLuminance = 0.3f;

    [Header("Exposure Settings")]    
    [Tooltip("Minimum exposure value (darker limit)")]
    [Range(-10f, 0f)]
    public float minExposure = -4f;
    
    [Tooltip("Maximum exposure value (brighter limit)")]
    [Range(0f, 10f)]
    public float maxExposure = 4f;

    [Header("Physical Camera Settings")]
    [Tooltip("F-stop of the camera (lower values = brighter, like security cameras)")]
    [Range(0.7f, 32f)]
    public float aperture = 1.8f;

    [Tooltip("Shutter speed in seconds (higher = more light but more motion blur)")]
    [Range(1/8000f, 1f)]
    public float shutterSpeed = 1/30f;

    [Tooltip("ISO sensitivity (higher = brighter but more noise)")]
    [Range(100f, 12800f)]
    public float iso = 3200f;

    [Header("Sampling Settings")]
    [Tooltip("Resolution for luminance calculation (power of 2, higher = more accurate, lower = better performance)")]
    [Range(64, 512)]
    public int samplingResolution = 256;
    
    [Tooltip("Weight given to the center of the screen (higher = more focus on center)")]
    [Range(1f, 5f)]
    public float centerWeight = 2.0f;

    private Volume postProcessVolume;
    private ColorAdjustments colorAdjustments;
    private float currentExposure = 0f;
    
    private ComputeBuffer luminanceBuffer;
    private RenderTexture sourceTexture;
    private int kernelHandle;
    private Camera mainCamera;
    private float[] luminanceData;
    
    [Header("Debug Settings")]
    [Tooltip("Show debug information on screen")]
    public bool showDebug = true;
    
    [Tooltip("Test the adaptation with an oscillating exposure")]
    public bool testMode = false;
    
    private float debugTimer = 0f;
    private Color debugColor = Color.white;

    void Start()
    {
        if (computeShader == null)
        {
            Debug.LogError("Please assign a compute shader to the Eye Adaptation component!");
            enabled = false;
            return;
        }

        // Setup post processing
        SetupPostProcessing();

        // Setup compute shader resources
        SetupComputeResources();
        
        // Get main camera
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("No main camera found!");
            enabled = false;
            return;
        }
    }

    void SetupPostProcessing()
    {
        postProcessVolume = GetComponent<Volume>();
        if (postProcessVolume == null)
        {
            postProcessVolume = gameObject.AddComponent<Volume>();
            postProcessVolume.isGlobal = true;
        }

        if (postProcessVolume.profile == null)
        {
            postProcessVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        }

        if (!postProcessVolume.profile.TryGet<ColorAdjustments>(out colorAdjustments))
        {
            colorAdjustments = postProcessVolume.profile.Add<ColorAdjustments>(false);
        }

        currentExposure = colorAdjustments.postExposure.value;
    }

    void SetupComputeResources()
    {
        // Get the kernel index
        kernelHandle = computeShader.FindKernel("CSMain");

        // Create source texture
        sourceTexture = new RenderTexture(samplingResolution, samplingResolution, 0, RenderTextureFormat.ARGBFloat);
        sourceTexture.enableRandomWrite = true;
        sourceTexture.Create();

        // Create luminance buffer
        luminanceBuffer = new ComputeBuffer(samplingResolution * samplingResolution, sizeof(float));
        luminanceData = new float[samplingResolution * samplingResolution];
    }

    void Update()
    {
        if (!enabled || mainCamera == null || computeShader == null) return;

        // Copy screen to our source texture
        Graphics.Blit(null, sourceTexture);

        // Set compute shader parameters
        computeShader.SetTexture(kernelHandle, "_SourceTex", sourceTexture);
        computeShader.SetBuffer(kernelHandle, "_LuminanceBuffer", luminanceBuffer);
        computeShader.SetFloat("_CenterWeight", centerWeight);
        computeShader.SetVector("_ScreenSize", new Vector2(samplingResolution, samplingResolution));

        // Dispatch compute shader
        computeShader.Dispatch(kernelHandle, samplingResolution / 8, samplingResolution / 8, 1);

        // Get results
        luminanceBuffer.GetData(luminanceData);

        // Calculate average luminance
        float totalLuminance = 0f;
        foreach (float lum in luminanceData)
        {
            totalLuminance += lum;
        }
        float averageLuminance = totalLuminance / luminanceData.Length;

        // Calculate target exposure with more dramatic response
        float targetExposure = -Mathf.Log(Mathf.Max(averageLuminance, 0.0001f));
        
        // Apply non-linear response curve for more dramatic adaptation
        targetExposure *= 1.5f; // Amplify the effect
        targetExposure = Mathf.Sign(targetExposure) * Mathf.Pow(Mathf.Abs(targetExposure), 1.2f);
        
        // Add slight oscillation for camera-like behavior
        float noise = Mathf.PerlinNoise(Time.time * 2.0f, 0) * 0.1f - 0.05f;
        targetExposure += noise;

        // Smoothly adapt current exposure with more aggressive speeds
        // Test mode: override with oscillating exposure
        if (testMode)
        {
            debugTimer += Time.deltaTime;
            targetExposure = Mathf.Sin(debugTimer * 0.5f) * 3f; // Oscillate between bright and dark
        }

        float adaptSpeed = targetExposure > currentExposure ? 
            adaptationSpeedToBright * (1.0f + Mathf.Abs(targetExposure - currentExposure)) : 
            adaptationSpeedToDark * (1.0f + Mathf.Abs(targetExposure - currentExposure));
        
        currentExposure = Mathf.Lerp(currentExposure, targetExposure, 
            Time.deltaTime * adaptSpeed);

        // Update debug color based on exposure
        debugColor = currentExposure > 0 ? 
            new Color(1, 1, 0, Mathf.Abs(currentExposure) / maxExposure) : // Yellow for bright
            new Color(0, 0, 1, Mathf.Abs(currentExposure) / Mathf.Abs(minExposure)); // Blue for dark

        if (showDebug)
        {
            ShowDebugInfo(averageLuminance, targetExposure, adaptSpeed);
        }

        // Clamp and apply exposure
        currentExposure = Mathf.Clamp(currentExposure, minExposure, maxExposure);
        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.value = currentExposure;
        }
    }

    void ShowDebugInfo(float avgLuminance, float targetExp, float adaptSpeed)
    {
        // Create a background box
        GUI.backgroundColor = new Color(0, 0, 0, 0.8f);
        GUI.Box(new Rect(10, 10, 250, 160), "Eye Adaptation Debug");

        // Draw the debug information
        GUI.backgroundColor = Color.white;
        GUI.contentColor = Color.white;
        
        GUILayout.BeginArea(new Rect(20, 30, 240, 150));
        
        GUILayout.Label($"Average Luminance: {avgLuminance:F3}");
        GUILayout.Label($"Current Exposure: {currentExposure:F2}");
    GUILayout.Label($"Target Exposure: {targetExp:F2}");
    GUILayout.Label($"Adaptation Speed: {adaptSpeed:F2}");
        
    // Draw exposure bar (runtime-safe replacement for EditorGUI.ProgressBar)
    float normalizedExposure = (currentExposure - minExposure) / (maxExposure - minExposure);
    normalizedExposure = Mathf.Clamp01(normalizedExposure);
    // Background for bar
    GUI.color = Color.black * new Color(1,1,1,0.6f);
    GUI.Box(new Rect(10, 100, 230, 20), "");
    // Filled portion
    GUI.color = Color.Lerp(Color.red, Color.green, normalizedExposure);
    GUI.Box(new Rect(10, 100, 230 * normalizedExposure, 20), "");
    GUI.color = Color.white;
    GUI.Label(new Rect(12, 100, 230, 20), $"Exposure: {(normalizedExposure*100f):F0}%");

    // Visual indicator
    GUI.backgroundColor = debugColor;
    GUI.Box(new Rect(10, 125, 230, 15), "");
        
        GUILayout.EndArea();
    }

    void OnDestroy()
    {
        if (sourceTexture != null)
        {
            sourceTexture.Release();
            sourceTexture = null;
        }

        if (luminanceBuffer != null)
        {
            luminanceBuffer.Release();
            luminanceBuffer = null;
        }
    }
}
