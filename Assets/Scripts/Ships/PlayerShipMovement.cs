using UnityEngine;

namespace Ships
{
    public class PlayerShipMovement : MonoBehaviour
    {
        [Header("Runtime")]
        public Vector2 velocity;

        private Vector2 _desiredDirection;

        private ShipBase _ship;
        private PlayerInputSystem _hw;
        private PlayerInputUI _ui;

        // ---------------------------
        // Tuneable behaviour
        // ---------------------------
        [Header("Turning Behaviour")]
        [SerializeField] private float gentleTurnAngle = 15f;   // малый угол — корректировка без потери скорости
        [SerializeField] private float mediumTurnAngle = 45f;   // средний угол — немного тормозим
        //[SerializeField] private float rotationSpeed = 180f;    // скорость поворота корпуса (deg/sec)
        [SerializeField] private float velocityAlignRate = 8f;  // как быстро velocity догоняет forward

        private void Awake()
        {
            _ship = GetComponent<ShipBase>();
            _hw = FindObjectOfType<PlayerInputSystem>();
            _ui = FindObjectOfType<PlayerInputUI>();
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
            var hwDir = _hw != null ? _hw.Steering : Vector2.zero;
            var uiDir = _ui != null ? _ui.SteeringUI : Vector2.zero;

            var sum = hwDir + uiDir;

            if (sum.sqrMagnitude > 0.001f)
                _desiredDirection = sum.normalized;
            else
                _desiredDirection = Vector2.zero;
        }

        // ============================================================
        // ROTATION (Forward главный)
        // ============================================================
        private void HandleRotation()
        {
            if (_desiredDirection.sqrMagnitude < 0.001f)
                return;

            var targetAngle = Mathf.Atan2(_desiredDirection.y, _desiredDirection.x) * Mathf.Rad2Deg - 90f;
            var currentAngle = transform.eulerAngles.z;

            transform.rotation = Quaternion.RotateTowards(
                Quaternion.Euler(0, 0, currentAngle),
                Quaternion.Euler(0, 0, targetAngle),
                _ship.GetStat(StatType.TurnSpeed).Current * Time.deltaTime
            );
        }

        // ============================================================
        // MOVEMENT
        // ============================================================
        private void HandleMovement()
        {
            var maxSpeed   = _ship.GetStat(StatType.MoveSpeed).Current;
            var accel      = _ship.GetStat(StatType.Acceleration).Current;
            var brakePower = _ship.GetStat(StatType.BrakePower).Current;

            var currentSpeed = velocity.magnitude;
            var forward = transform.up;
            var hasInput = _desiredDirection.sqrMagnitude > 0.001f;

            // -------------------------------
            // Determine angle change
            // -------------------------------
            float angle = hasInput
                ? Vector2.Angle(forward, _desiredDirection)
                : 0f;

            float targetSpeed;

            // ============================================================
            // CASE 1 — малый угол → не тормозим, только корректируем
            // ============================================================
            if (hasInput && angle < gentleTurnAngle)
            {
                targetSpeed = maxSpeed;
            }
            // ============================================================
            // CASE 2 — средний угол → немного тормозим
            // ============================================================
            else if (hasInput && angle < mediumTurnAngle)
            {
                targetSpeed = Mathf.Lerp(currentSpeed, maxSpeed, 0.5f);
            }
            // ============================================================
            // CASE 3 — резкий манёвр → серьёзное торможение
            // ============================================================
            else if (hasInput)
            {
                targetSpeed = 0f; // почти остановка перед разворотом
            }
            else
            {
                // игрок отпустил управление → тормоз
                targetSpeed = 0f;
            }

            // -------------------------------
            // Accelerate / brake toward targetSpeed
            // -------------------------------
            if (targetSpeed > currentSpeed)
            {
                currentSpeed = Mathf.MoveTowards(
                    currentSpeed,
                    targetSpeed,
                    accel * Time.deltaTime
                );
            }
            else
            {
                currentSpeed = Mathf.MoveTowards(
                    currentSpeed,
                    targetSpeed,
                    brakePower * Time.deltaTime
                );
            }

            // -------------------------------
            // Velocity follows forward
            // -------------------------------
            if (velocity.sqrMagnitude > 0.001f)
            {
                var newDir = Vector2.Lerp(
                    velocity.normalized,
                    forward,
                    velocityAlignRate * Time.deltaTime
                ).normalized;

                velocity = newDir * currentSpeed;
            }
            else
            {
                velocity = forward * currentSpeed;
            }

            // -------------------------------
            // Apply movement
            // -------------------------------
            transform.position += (Vector3)velocity * Time.deltaTime;

            if (Battle.Instance != null)
            {
                var clamped = Battle.Instance.ClampPosition(transform.position);
                transform.position = clamped;

                if ((Vector2)transform.position != clamped)
                    velocity = Vector2.zero;
            }
        }
    }
}
