using UnityEngine;

public class PlayerDelivery : MonoBehaviour
{
    public Transform playerCamera; // for arrow rotation relative to camera
    public GameUIManager uiManager;
    public UnityEngine.UI.Image arrowUI;
    public float deliverDistance = 3f;

    private DeliveryTarget currentTarget;

    void Update()
    {
        // Find closest undelivered target
        FindClosestTarget();

        if (currentTarget != null)
        {
            // Rotate arrow relative to camera
            Vector3 dir = currentTarget.transform.position - playerCamera.position;
            Vector3 flatDir = new Vector3(dir.x, 0, dir.z).normalized;
            float angle = Vector3.SignedAngle(playerCamera.forward, flatDir, Vector3.up);
            arrowUI.rectTransform.localEulerAngles = new Vector3(0, 0, -angle + 90);

            // Deliver if close and E pressed
            if (Vector3.Distance(transform.position, currentTarget.transform.position) <= deliverDistance
                && Input.GetKeyDown(KeyCode.E))
            {
                currentTarget.Deliver();
                uiManager.PizzaDelivered();
            }
        }

        // Hide arrow if all delivered
        arrowUI.gameObject.SetActive(currentTarget != null);

                // Check if all pizzas delivered
        bool allDelivered = true;
        DeliveryTarget[] allTargets = FindObjectsByType<DeliveryTarget>(FindObjectsSortMode.None);

        foreach (var t in allTargets)
        {
            if (!t.IsDelivered)
            {
                allDelivered = false;
                break;
            }
        }

        if (allDelivered)
        {
            uiManager.ShowSuccessUI();
        }

    }

    void FindClosestTarget()
    {
        DeliveryTarget[] allTargets = FindObjectsByType<DeliveryTarget>(FindObjectsSortMode.None);
        DeliveryTarget closest = null;
        float minDist = Mathf.Infinity;

        foreach (var t in allTargets)
        {
            if (t.IsDelivered) continue;

            float dist = Vector3.Distance(transform.position, t.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = t;
            }
        }

        currentTarget = closest;
    }
}
