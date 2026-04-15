using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerAttack : MonoBehaviour
{
    [Header("Настройки атаки")]
    public float attackDamage = 25f;
    public float attackCooldown = 1f;
    public LayerMask enemyLayers;
    public Vector3 hitboxSize = new Vector3(2f, 2f, 1f);
    public Vector3 hitboxOffset = new Vector3(1f, 0f, 0f);

    [Header("📱 Свайпы")]
    public float minSwipeDistance = 50f;
    public float maxSwipeTime = 0.5f;

    [Header("🎬 Аниматоры")]
    public Animator playerAnimator;   // knight (принимает направление)
    public Animator swingAnimator;    // square (принимает attack)

    [Header("⚙️ Параметры Animator")]
    public string playerParamRight = "AttackRight";
    public string playerParamLeft = "AttackLeft";
    public string swingParamAttack = "attack";

    [Header("🔍 Отладка")]
    public bool debugLogs = true;

    private float cooldownTimer;
    private PlayerControls playerControls;
    private PlayerMovement playerMovement;
    private bool isAttacking;

    // Мультитач трекинг
    private Dictionary<int, Vector2> atkStartPos = new();
    private Dictionary<int, float> atkStartTime = new();

    void Awake()
    {
        playerControls = new PlayerControls();
        playerMovement = GetComponent<PlayerMovement>();
        if (playerAnimator == null) playerAnimator = GetComponent<Animator>();
        if (swingAnimator == null) swingAnimator = GetComponentInChildren<Animator>();
    }

    void OnEnable()
    {
        playerControls?.Enable();
        playerControls.Player.Attack.performed += ctx => TriggerSwingAnimation();
    }

    void OnDisable()
    {
        playerControls?.Disable();
        playerControls.Player.Attack.performed -= ctx => TriggerSwingAnimation();
    }

    void Update()
    {
        // 1. Кулдаун и сброс анимации после удара
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
            if (isAttacking && cooldownTimer < attackCooldown / 2)
            {
                DetectAndDamageEnemies();
                isAttacking = false;
                ResetAnimation();
            }
        }

        // 2. ПК: Простая атака на ЛКМ (без расчёта направления)
#if UNITY_STANDALONE || UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            TriggerSwingAnimation();
            TryAttack();
        }
#endif

        // 3. Мобильные: Свайпы (с расчётом направления для playerAnimator)
        HandleSwipeInput();
    }

    void HandleSwipeInput()
    {
        CleanTouchDict(atkStartPos, atkStartTime);

        for (int i = 0; i < UnityEngine.Input.touchCount; i++)
        {
            var touch = UnityEngine.Input.GetTouch(i);
            if (EventSystem.current.IsPointerOverGameObject(touch.fingerId)) continue;

            if (touch.phase == UnityEngine.TouchPhase.Began)
            {
                atkStartPos[touch.fingerId] = touch.position;
                atkStartTime[touch.fingerId] = Time.time;
            }
            else if (touch.phase == UnityEngine.TouchPhase.Ended && atkStartPos.TryGetValue(touch.fingerId, out Vector2 start))
            {
                Vector2 delta = touch.position - start;
                float duration = Time.time - atkStartTime[touch.fingerId];

                // Горизонтальный свайп = атака с направлением
                if (duration < maxSwipeTime && delta.magnitude > minSwipeDistance && Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                {
                    bool isRight = delta.x > 0;
                    PlayAttackDirectional(isRight);
                    TryAttack();
                }
                atkStartPos.Remove(touch.fingerId);
                atkStartTime.Remove(touch.fingerId);
            }
        }
    }

    // Только запуск взмаха (для ПК или когда направление не нужно)
    void TriggerSwingAnimation()
    {
        if (swingAnimator == null) return;
        swingAnimator.SetBool(swingParamAttack, true);
        if (debugLogs) Debug.Log("⚔️ ПК: attack = true");
    }

    // Атака с направлением (для мобильных свайпов)
    void PlayAttackDirectional(bool isRight)
    {
        // Направление для рыцаря
        if (playerAnimator != null)
        {
            playerAnimator.SetBool(playerParamLeft, false);
            playerAnimator.SetBool(playerParamRight, false);
            playerAnimator.SetBool(isRight ? playerParamRight : playerParamLeft, true);
        }

        // Взмах для квадрата
        if (swingAnimator != null)
        {
            swingAnimator.SetBool(swingParamAttack, true);
        }
    }

    void ResetAnimation()
    {
        if (swingAnimator != null)
        {
            swingAnimator.SetBool(swingParamAttack, false);
        }
    }

    void CleanTouchDict(Dictionary<int, Vector2> pos, Dictionary<int, float> time)
    {
        var remove = new List<int>();
        foreach (var id in pos.Keys)
        {
            bool active = false;
            for (int i = 0; i < UnityEngine.Input.touchCount; i++)
                if (UnityEngine.Input.GetTouch(i).fingerId == id) { active = true; break; }
            if (!active) remove.Add(id);
        }
        foreach (var id in remove) { pos.Remove(id); time.Remove(id); }
    }

    void TryAttack()
    {
        if (cooldownTimer > 0 || (playerMovement != null && playerMovement.IsDashing)) return;
        PerformAttack();
    }

    void PerformAttack()
    {
        cooldownTimer = attackCooldown;
        isAttacking = true;
    }

    void DetectAndDamageEnemies()
    {
        Vector3 center = transform.position + hitboxOffset;
        Collider[] hits = Physics.OverlapBox(center, hitboxSize / 2, Quaternion.identity, enemyLayers);
        foreach (var hit in hits)
            if (hit.TryGetComponent(out EnemyHealth enemy))
                enemy.TakeDamage(attackDamage, hit.transform.position - transform.position);
    }
}