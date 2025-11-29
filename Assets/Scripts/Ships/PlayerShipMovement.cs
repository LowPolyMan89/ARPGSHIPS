using UnityEngine;

namespace Ships
{
    public class PlayerShipMovement : MonoBehaviour
    {
        [Header("Runtime")] public Vector2 velocity;
        public float throttleCache = 0f;

        [Header("Settings")] public float throttleStep = 0.7f;

        private ShipBase ship;

        private PlayerInputSystem hw;
        private PlayerInputUI ui;

        private Vector2 desiredDirection;
        private float lastSliderValue;

        private void Awake()
        {
            ship = GetComponent<ShipBase>();
            hw = FindObjectOfType<PlayerInputSystem>();
            ui = FindObjectOfType<PlayerInputUI>();
        }

        private void Update()
        {
            ReadInput();
            HandleRotation();
            HandleMovement();
        }

        // ============================================================
        // INPUT
        // ============================================================
        private void ReadInput()
        {
            // ---------------------------------------------------------
            // 1) Steering = HW + UI
            // ---------------------------------------------------------
            Vector2 steeringHW = hw != null ? hw.Steering : Vector2.zero;
            Vector2 steeringUI = ui != null ? ui.SteeringUI : Vector2.zero;

            desiredDirection = (steeringHW + steeringUI).normalized;

            // ---------------------------------------------------------
            // 2) THROTTLE CACHE
            // ---------------------------------------------------------

            // + / - от клавиатуры
            float axis = hw != null ? hw.Throttle : 0f;

            if (Mathf.Abs(axis) > 0.01f)
            {
                throttleCache += axis * throttleStep * Time.deltaTime;
            }

            // UI слайдер — задаёт throttleCache напрямую
            if (ui != null)
            {
                float sliderValue = ui.SliderValue;

                if (!Mathf.Approximately(sliderValue, lastSliderValue))
                {
                    throttleCache = sliderValue; // пользователь двинул слайдер
                }

                ui.SetSlider(throttleCache); // отобразить состояние
                lastSliderValue = sliderValue;
            }

            throttleCache = Mathf.Clamp01(throttleCache);
        }

        // ============================================================
        // ROTATION
        // ============================================================
        private void HandleRotation()
        {
            if (desiredDirection.sqrMagnitude < 0.0005f)
                return;

            float turnSpeed = ship.GetStat(StatType.TurnSpeed).Current;

            float targetAngle = Mathf.Atan2(desiredDirection.y, desiredDirection.x) * Mathf.Rad2Deg - 90f;

            float angle = Mathf.LerpAngle(
                transform.eulerAngles.z,
                targetAngle,
                turnSpeed * Time.deltaTime
            );

            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        // ============================================================
        // MOVEMENT (вискозное движение)
        // ============================================================
        private void HandleMovement()
        {
            float maxSpeed = ship.GetStat(StatType.MoveSpeed).Current;
            float accel = ship.GetStat(StatType.Acceleration).Current;
            float brakePower = ship.GetStat(StatType.BrakePower).Current;
            float turnInertia = 6f; // как быстро velocity поворачивается к носу

            Vector2 forward = transform.up;

            float targetSpeed = throttleCache * maxSpeed;
            float currentSpeed = velocity.magnitude;

            // -------------------------------
            // 1) Ускорение / Торможение
            // -------------------------------
            if (targetSpeed > currentSpeed)
            {
                // разгон
                currentSpeed = Mathf.MoveTowards(
                    currentSpeed,
                    targetSpeed,
                    accel * Time.deltaTime
                );
            }
            else
            {
                // торможение (всегда через brakePower!)
                currentSpeed = Mathf.MoveTowards(
                    currentSpeed,
                    targetSpeed,
                    brakePower * Time.deltaTime
                );
            }

            // -------------------------------
            // 2) Поворот velocity в сторону forward
            // -------------------------------
            if (velocity.sqrMagnitude > 0.001f)
            {
                Vector2 currentDir = velocity.normalized;
                Vector2 newDir = Vector2.Lerp(currentDir, forward, turnInertia * Time.deltaTime).normalized;

                velocity = newDir * currentSpeed;
            }
            else
            {
                velocity = forward * currentSpeed;
            }

            // -------------------------------
            // 3) Перемещение
            // -------------------------------
            transform.position += (Vector3)velocity * Time.deltaTime;
        }
    }
}
