using UnityEngine;

public class gamemanager : MonoBehaviour
{
    void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 144;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Initialize game state
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
