using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Player))]
public class PlayerUI : MonoBehaviour
{
    Player player;

    void Start()
    {
        player = GetComponent<Player>();
    }

    [Header("Health Bar")]
    [SerializeField] private Image healthBarFill;

    [Header("Dash Cooldown Bar")]
    [SerializeField] private Image dashBarFill;
    [SerializeField] private Color dashReadyColor = Color.white;
    [SerializeField] private Color dashBlockedColor = new(1f, 1f, 1f, 0.3f);
    [SerializeField] private Color dashCooldownColor = Color.gray;

    [SerializeField] private GameObject restartText;

    void Update()
    {
        UpdateHealthBar();
        UpdateDashBar();
        UpdateRestart();
    }

    private void UpdateRestart()
    {
        if (player.IsDead)
        {
            restartText.SetActive(true);
        }
        else
        {
            restartText.SetActive(false);
        }
    }

    private void UpdateHealthBar()
    {
        float healthPercent = player.health / player.MaxHealth;
        healthBarFill.fillAmount = Mathf.Clamp01(healthPercent);
    }

    private void UpdateDashBar()
    {
        float fill = player.TimeSinceLastDash / player.dashCooldown;
        dashBarFill.fillAmount = Mathf.Clamp01(fill);

        if (fill < 1f)
        {
            dashBarFill.color = dashCooldownColor;
        }
        else if (player.CanDash)
        {
            dashBarFill.color = dashReadyColor;
        }
        else
        {
            dashBarFill.color = dashBlockedColor;
        }
    }
}
