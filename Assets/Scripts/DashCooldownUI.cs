using UnityEngine;
using UnityEngine.UI;

public class DashCooldownUI : MonoBehaviour
{
    public Image fillImage; // set to an Image with Fill type = Filled
    public AdvancedMovement movement; // assign player script

    void Start()
    {
        if (movement == null)
            movement = Object.FindFirstObjectByType<AdvancedMovement>();

        // Listen to the dash cooldown event
        movement.OnDashCooldownChange.AddListener(UpdateDashUI);

    }

    void OnDestroy()
    {
        movement.OnDashCooldownChange.RemoveListener(UpdateDashUI);
    }

    // This method updates the UI
    void UpdateDashUI(float timeLeft)
    {
        // Find the longest cooldown among charges
        float nextChargeTime = 0f;
        for (int i = 0; i < movement.dashChargeTimers.Length; i++)
            if (movement.dashChargeTimers[i] > nextChargeTime)
                nextChargeTime = movement.dashChargeTimers[i];

        float normalized = Mathf.Clamp01(nextChargeTime / movement.dashRechargeDelay);
        if (fillImage != null)
            fillImage.fillAmount = 1f - normalized; // fill increases as cooldown finishes
    }

}
