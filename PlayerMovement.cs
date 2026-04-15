using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("⚙️ Настройки")]
    public float moveSpeed = 5f;
    public bool lockRotation = true;
    public float fixedAngle = -90f;

    [Header("🕹️ Джойстик")]
    public Joystick mobileJoystick; // Перетащи сюда объект джойстика

    [Header("⚡ Дэш")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    [Header("📱 Свайп для дэша")]
    public float minSwipeDistance = 50f;
    public float maxSwipeTime = 0.5f;

    [Header("🍎 Гравитация")]
    public float gravity = -9.81f; // Сила притяжения (можно увеличить для "аркадности")
    private Vector3 velocity;      // Вертикальная скорость

    private CharacterController controller;
    private Vector3 moveDirection;
    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private PlayerControls playerControls;

    // Мультитач трекинг для свайпов
    private Dictionary<int, Vector2> swipeStartPos = new();
    private Dictionary<int, float> swipeStartTime = new();

    public bool IsDashing => isDashing;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerControls = new PlayerControls();
        if (mobileJoystick == null) mobileJoystick = FindObjectOfType<Joystick>();
    }

    void OnEnable() => playerControls?.Enable();
    void OnDisable() => playerControls?.Disable();

    void Update()
    {
        if (dashCooldownTimer > 0) dashCooldownTimer -= Time.deltaTime;

        // Логика дэша (приоритет)
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            // Во время дэша двигаемся строго по направлению дэша
            controller.Move(moveDirection.normalized * dashSpeed * Time.deltaTime);
            if (dashTimer <= 0) isDashing = false;
            return; // Прерываем Update, чтобы не применять гравитацию/ходьбу во время дэша
        }

        // ПК: Дэш на Shift
#if UNITY_STANDALONE || UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            PerformDash(moveDirection);
#endif

        // ЧТЕНИЕ ДВИЖЕНИЯ
        Vector2 input = mobileJoystick != null
            ? new Vector2(mobileJoystick.Horizontal, mobileJoystick.Vertical)
            : playerControls.Player.Move.ReadValue<Vector2>();

        moveDirection = new Vector3(input.x, 0, input.y).normalized;

        // Мобильный: Свайп вниз для дэша
        HandleSwipeDash();

        // ПРИМЕНЕНИЕ ГРАВИТАЦИИ
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Прижимаем к земле, чтобы не проваливаться
        }
        else
        {
            velocity.y += gravity * Time.deltaTime; // Применяем гравитацию в воздухе
        }

        // ФИНАЛЬНОЕ ДВИЖЕНИЕ (Горизонталь + Гравитация)
        Vector3 finalMove = new Vector3(moveDirection.x, velocity.y, moveDirection.z) * moveSpeed;

        if (moveDirection.magnitude > 0.1f || !controller.isGrounded)
        {
            transform.rotation = lockRotation
                ? Quaternion.Euler(0, 0, fixedAngle)
                : Quaternion.LookRotation(new Vector3(moveDirection.x, 0, moveDirection.z)); // Игнорируем Y для поворота

            controller.Move(finalMove * Time.deltaTime);
        }
    }

    void HandleSwipeDash()
    {
        CleanTouchDict(swipeStartPos, swipeStartTime);

        for (int i = 0; i < UnityEngine.Input.touchCount; i++)
        {
            var touch = UnityEngine.Input.GetTouch(i);
            if (EventSystem.current.IsPointerOverGameObject(touch.fingerId)) continue;

            if (touch.phase == UnityEngine.TouchPhase.Began)
            {
                swipeStartPos[touch.fingerId] = touch.position;
                swipeStartTime[touch.fingerId] = Time.time;
            }
            else if (touch.phase == UnityEngine.TouchPhase.Ended && swipeStartPos.TryGetValue(touch.fingerId, out Vector2 start))
            {
                Vector2 delta = touch.position - start;
                float duration = Time.time - swipeStartTime[touch.fingerId];

                if (duration < maxSwipeTime && delta.magnitude > minSwipeDistance &&
                    Mathf.Abs(delta.y) > Mathf.Abs(delta.x) && delta.y < 0)
                {
                    PerformDash(moveDirection);
                }
                swipeStartPos.Remove(touch.fingerId);
                swipeStartTime.Remove(touch.fingerId);
            }
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

    public void PerformDash(Vector3 dir)
    {
        if (isDashing || dashCooldownTimer > 0) return;
        moveDirection = dir.magnitude > 0.1f ? dir : Vector3.right;
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
    }
}