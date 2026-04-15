using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Здоровье")]
    [SerializeField] private int maxHealthSegments = 5;
    [SerializeField] private int currentHealthSegments;
    [SerializeField] private float damagePerSegment = 10f;

    [Header("Полоска здоровья")]
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private bool showHealthBar = true;

    [Header("Неуязвимость")]
    [SerializeField] private float invincibilityDuration = 0.5f;

    [Header("Отладка")]
    [SerializeField] private bool showDebugLogs = false;

    private EnemyHealthBar healthBarInstance;
    private bool isDead = false;
    private float invincibilityTimer = 0f;

    void Start()
    {
        currentHealthSegments = maxHealthSegments;

        if (showDebugLogs) Debug.Log($"[{gameObject.name}] Start! HP: {currentHealthSegments}/{maxHealthSegments}");

        if (showHealthBar && healthBarPrefab != null)
        {
            GameObject bar = Instantiate(healthBarPrefab, transform.position + Vector3.up * 2.5f, Quaternion.identity);
            healthBarInstance = bar.GetComponent<EnemyHealthBar>();

            if (healthBarInstance != null)
            {
                healthBarInstance.target = transform;
                healthBarInstance.SetMaxSegments(maxHealthSegments);
                healthBarInstance.UpdateHealth(currentHealthSegments, maxHealthSegments);

                if (showDebugLogs) Debug.Log("Полоска создана!");
            }
        }
    }

    void Update()
    {
        if (invincibilityTimer > 0) invincibilityTimer -= Time.deltaTime;
    }

    /// <summary>
    /// Нанести урон
    /// </summary>
    public void TakeDamage(float damage, Vector3 hitDirection = default)
    {
        if (showDebugLogs) Debug.Log($"[{gameObject.name}] Получил урон: {damage}");

        if (isDead || invincibilityTimer > 0) return;

        // Вычисляем сколько сегментов потерять
        int segmentsToLose = Mathf.CeilToInt(damage / damagePerSegment);
        currentHealthSegments = Mathf.Max(0, currentHealthSegments - segmentsToLose);

        if (showDebugLogs) Debug.Log($"[{gameObject.name}] HP: {currentHealthSegments}/{maxHealthSegments}");

        if (healthBarInstance != null && showHealthBar)
        {
            healthBarInstance.UpdateHealth(currentHealthSegments, maxHealthSegments);
        }

        if (currentHealthSegments <= 0) Die();
        else invincibilityTimer = invincibilityDuration;
    }

    /// <summary>
    /// Нанести урон
    /// </summary>
    public void TakeDamageOneSegment(Vector3 hitDirection = default)
    {
        TakeDamage(damagePerSegment, hitDirection);
    }

    /// <summary>
    /// Лечение
    /// </summary>
    public void Heal(int amount)
    {
        currentHealthSegments = Mathf.Min(maxHealthSegments, currentHealthSegments + amount);

        if (healthBarInstance != null && showHealthBar)
        {
            healthBarInstance.UpdateHealth(currentHealthSegments, maxHealthSegments);
        }

        if (showDebugLogs) Debug.Log($"[{gameObject.name}] вылечен на {amount}. HP: {currentHealthSegments}/{maxHealthSegments}");
    }

    void Die()
    {
        isDead = true;
        if (showDebugLogs) Debug.Log($"[{gameObject.name}] УМЕР!");

        if (healthBarInstance != null)
        {
            Destroy(healthBarInstance.gameObject);
        }

        Destroy(gameObject, 0.1f);
    }

    // Публичные геттеры для других скриптов
    public int CurrentHealth => currentHealthSegments;
    public int MaxHealth => maxHealthSegments;
    public float HealthPercent => (float)currentHealthSegments / maxHealthSegments;
}
