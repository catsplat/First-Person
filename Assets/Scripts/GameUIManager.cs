using UnityEngine;
using TMPro;

public class GameUIManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text timerText;
    public TMP_Text pizzaCountText;
    public TMP_Text checklistText;
    public TMP_Text deathCountText;
    public GameObject successUI;
    public GameObject failUI;

    [Header("Timer Settings")]
    public float timeRemaining = 120f;
    private bool timerRunning = true;

    private DeliveryTarget[] targets;
    private int totalPizzas = 0;
    private int deliveredPizzas = 0;
    private int deathCount = 0;

    void Start()
    {
        targets = FindObjectsByType<DeliveryTarget>(FindObjectsSortMode.None);
        totalPizzas = targets.Length;

        // Assign pizza numbers
        for (int i = 0; i < targets.Length; i++)
        {
            targets[i].pizzaNumber = i + 1;
        }

        UpdateTimerUI();
        UpdatePizzaUI();
        UpdateDeathUI();
    }

    void Update()
    {
        if (!timerRunning) return;

        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerUI();
        }
        else
        {
            timeRemaining = 0;
            timerRunning = false;
            ShowFailUI();
        }
    }

    void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    public void PizzaDelivered()
    {
        deliveredPizzas++;
        UpdatePizzaUI();

        if (deliveredPizzas >= totalPizzas)
            ShowSuccessUI();
    }

    void UpdatePizzaUI()
    {
        pizzaCountText.text = $"{deliveredPizzas}/{totalPizzas} pizzas delivered";

        checklistText.text = "";
        foreach (var target in targets)
        {
            string icon = target.IsDelivered ? "✅" : "☐";
            string color = target.IsDelivered ? "#00FF00" : "#000000";
            checklistText.text += $"<color={color}>{icon} Pizza {target.pizzaNumber}</color>\n";
        }
    }

    public void AddDeath()
    {
        deathCount++;
        UpdateDeathUI();
    }

    void UpdateDeathUI()
    {
        deathCountText.text = $"Deaths: {deathCount}";
    }

    public void ShowSuccessUI()
    {
        timerRunning = false;
        successUI.SetActive(true);
        failUI.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ShowFailUI()
    {
        successUI.SetActive(false);
        failUI.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
