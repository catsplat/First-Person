using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class gamemanager : MonoBehaviour
{
    [Header("Death Settings")]
    [Tooltip("Y position below which player dies")]
    [SerializeField] private float deathHeight = -5f;
    
    [Tooltip("Tag that kills player on contact")]
    [SerializeField] private string deadlyTag = "Deadly";

    [Header("Checkpoint Settings")]
    [Tooltip("Sound to play when reaching checkpoint")]
    [SerializeField] private AudioClip checkpointSound;
    
    [Tooltip("Should the last checkpoint be saved between game sessions?")]
    [SerializeField] private bool persistCheckpoints = true;

    [Header("References")]
    [Tooltip("Reference to the player's transform")]
    [SerializeField] private Transform playerTransform;

    // Cache spawn positions
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    
    // Checkpoint system
    private Transform currentCheckpoint;
    private List<Checkpoint> allCheckpoints = new List<Checkpoint>();
    private AudioSource audioSource;



    void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 144;

        // If player reference not set, try to find it
        if (playerTransform == null)
        {
            var playerCamera = Camera.main;
            if (playerCamera != null)
            {
                playerTransform = playerCamera.transform.parent;
            }
        }

        // Get or add audio source for checkpoint sounds
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && checkpointSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Find all checkpoints in the scene
        allCheckpoints.Add(FindFirstObjectByType<Checkpoint>());

    }

    void Start()
    {
        if (playerTransform != null)
        {
            // Store initial position and rotation for respawning
            initialPosition = playerTransform.position;
            initialRotation = playerTransform.rotation;

            // Reset checkpoint when entering a new scene
            if (currentCheckpoint != null && 
                PlayerPrefs.GetInt("LastCheckpointScene", -1) != SceneManager.GetActiveScene().buildIndex)
            {
                currentCheckpoint = null;
                PlayerPrefs.DeleteKey("LastCheckpoint");
                PlayerPrefs.DeleteKey("LastCheckpointScene");
                PlayerPrefs.Save();
                Debug.Log("New scene detected - checkpoint progress reset");
            }
        }
        else
        {
            Debug.LogWarning("Player transform not assigned to GameManager!");
        }
    }

    void Update()
    {
        CheckPlayerDeath();
    }

    void CheckPlayerDeath()
    {
        if (playerTransform != null)
        {
            // Check if player is below death height
            if (playerTransform.position.y < deathHeight)
            {
                PlayerDied();
            }
        }
    }

    void PlayerDied()
    {
        // Check if we're in the same scene as the checkpoint
        bool useCheckpoint = currentCheckpoint != null && 
                           SceneManager.GetActiveScene().buildIndex == PlayerPrefs.GetInt("LastCheckpointScene", -1);

        Debug.Log(useCheckpoint ? "Player died! Resetting to checkpoint..." : "Player died! Resetting to scene start...");
        
        if (playerTransform != null)
        {
            // Get respawn transform data
            Vector3 respawnPosition = useCheckpoint ? 
                currentCheckpoint.position : initialPosition;
            
            Quaternion respawnRotation = useCheckpoint ?
                currentCheckpoint.rotation : initialRotation;

            // Add small vertical offset to prevent ground clipping
            respawnPosition += Vector3.up * 0.1f;

            // Reset all physics components
            var rb = playerTransform.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.Sleep(); // Ensure physics system fully resets
            }

            // Reset player character controller if present
            var cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false; // Temporarily disable to allow teleport
            }

            // Reset position and rotation
            playerTransform.position = respawnPosition;
            playerTransform.rotation = respawnRotation;

            // Reset camera
            var playerCamera = playerTransform.GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                playerCamera.transform.localRotation = Quaternion.identity;
            }

            // Re-enable character controller
            if (cc != null)
            {
                cc.enabled = true;
            }

            // Reset any player movement script
            var movement = playerTransform.GetComponent<StarterAssets.FirstPersonController>();
            if (movement != null)
            {
                movement.enabled = false;
                movement.enabled = true; // Reset the controller's internal state
            }
        }
    }

    public void SetCheckpoint(Transform newCheckpoint)
    {
        // Deactivate old checkpoint visual
        if (currentCheckpoint != null)
        {
            var oldCP = currentCheckpoint.GetComponent<Checkpoint>();
            if (oldCP != null)
                oldCP.SetActive(false);
        }

        // Set and activate new checkpoint
        currentCheckpoint = newCheckpoint;
        var newCP = currentCheckpoint.GetComponent<Checkpoint>();
        if (newCP != null)
            newCP.SetActive(true);

        // Play checkpoint sound
        if (audioSource != null && checkpointSound != null)
        {
            audioSource.PlayOneShot(checkpointSound);
        }

        // Save checkpoint if persistence is enabled
        if (persistCheckpoints)
        {
            SaveCheckpoint();
        }

        Debug.Log("Checkpoint set at: " + newCheckpoint.position);
    }

    private void SaveCheckpoint()
    {
        if (currentCheckpoint != null)
        {
            // Save checkpoint index
            int checkpointIndex = allCheckpoints.FindIndex(cp => cp.transform == currentCheckpoint);
            if (checkpointIndex != -1)
            {
                PlayerPrefs.SetInt("LastCheckpoint", checkpointIndex);
                PlayerPrefs.SetInt("LastCheckpointScene", SceneManager.GetActiveScene().buildIndex);
                PlayerPrefs.Save();
            }
        }
    }

    private void LoadLastCheckpoint()
    {
        if (!persistCheckpoints) return;

        int lastScene = PlayerPrefs.GetInt("LastCheckpointScene", -1);
        if (lastScene == SceneManager.GetActiveScene().buildIndex)
        {
            int checkpointIndex = PlayerPrefs.GetInt("LastCheckpoint", -1);
            if (checkpointIndex >= 0 && checkpointIndex < allCheckpoints.Count)
            {
                SetCheckpoint(allCheckpoints[checkpointIndex].transform);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if player touched something deadly
        if (other.CompareTag(deadlyTag))
        {
            PlayerDied();
        }
    }
}
