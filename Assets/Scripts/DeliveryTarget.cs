using UnityEngine;

public class DeliveryTarget : MonoBehaviour
{
    [HideInInspector] public bool IsDelivered = false;
    public int pizzaNumber; // 1, 2, 3…

    public void Deliver()
    {
        if (IsDelivered) return;
        IsDelivered = true;

        // Hide the target so it can't be delivered again
        gameObject.SetActive(false);
    }
}
