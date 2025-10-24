using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    [Tooltip("Should this checkpoint be activated when the player touches it?")]
    [SerializeField] private bool activateOnTouch = true;

    [Header("Visual Feedback")]
    [Tooltip("Optional visual to show when checkpoint is active")]
    [SerializeField] private GameObject activeVisual;
    
    [Tooltip("Optional visual to show when checkpoint is inactive")]
    [SerializeField] private GameObject inactiveVisual;

    private bool isActive = false;

    private void Start()
    {
        UpdateVisuals();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!activateOnTouch) return;

        // Check if it's the player
        if (other.CompareTag("Player"))
        {
            // Get the game manager and set this as active checkpoint
            var gameManager = FindFirstObjectByType<gamemanager>();
            if (gameManager != null)
            {
                gameManager.SetCheckpoint(transform);
            }
        }
    }

    public void SetActive(bool active)
    {
        isActive = active;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (activeVisual != null)
            activeVisual.SetActive(isActive);
        if (inactiveVisual != null)
            inactiveVisual.SetActive(!isActive);
    }
}