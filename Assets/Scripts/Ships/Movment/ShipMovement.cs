using UnityEngine;

namespace Ships
{
    public class ShipMovement : MonoBehaviour
    {
        [Header("Runtime Debug")]
        public float currentSpeed;      // текущая линейная скорость
        public float currentTurnSpeed;  // скорость поворота корпуса
        public Vector3 velocity;        // вектор движения

        private ShipBase _tank;
        private PlayerInputSystem _hw;
        private PlayerInputUI _ui;
        private ShipCollisionKinematic _collision;

        private float _inputForward;    // W/S (-1..1)
        private float _inputTurn;       // A/D (-1..1)

        private void Awake()
        {
            _tank = GetComponent<ShipBase>();
            _collision = GetComponent<ShipCollisionKinematic>();

            _hw = FindObjectOfType<PlayerInputSystem>();
            _ui = FindObjectOfType<PlayerInputUI>();
        }

        private void Update()
        {
            HandleRotation();
            HandleMovement();
        }
        public void SetInput(float forward, float turn)
        {
            _inputForward = Mathf.Clamp(forward, -1f, 1f);
            _inputTurn = Mathf.Clamp(turn, -1f, 1f);
        }
        // =====================================================================
        // ROTATION
        // =====================================================================
        private void HandleRotation()
        {
            float turnStat = _tank.GetStat(StatType.TurnSpeed).Current;
            float turnSpeed = turnStat * _inputTurn;

            if (Mathf.Abs(turnSpeed) < 0.001f)
                return;

            Quaternion nextRot = transform.rotation * Quaternion.Euler(0f, turnSpeed * Time.deltaTime, 0f);

            // Проверяем, что при таком повороте танк не войдёт в стену
            if (_collision != null)
                _collision.PrepareForRotation();

            if (_collision != null &&
                _collision.RotationBlocked(transform.position, nextRot))
            {
                return;
            }

            transform.rotation = nextRot;
        }

        // =====================================================================
        // MOVEMENT
        // =====================================================================
        private void HandleMovement()
        {
            float accel = _tank.GetStat(StatType.Acceleration).Current;
            float brake = _tank.GetStat(StatType.BrakePower).Current;
            float forwardMax = _tank.GetStat(StatType.MoveSpeed).Current;
            float reverseMax = _tank.GetStat(StatType.MoveSpeedRear).Current;

            float targetSpeed = 0f;

            // желаемая скорость
            if (_inputForward > 0.01f)
                targetSpeed = forwardMax * _inputForward;
            else if (_inputForward < -0.01f)
                targetSpeed = reverseMax * _inputForward;
            else
                targetSpeed = 0f;

            // разгон / торможение
            currentSpeed = Mathf.MoveTowards(
                currentSpeed,
                targetSpeed,
                (Mathf.Abs(targetSpeed) > Mathf.Abs(currentSpeed) ? accel : brake) * Time.deltaTime
            );

            // вектор движения
            velocity = transform.forward * currentSpeed;

            // кандидат на позицию
            Vector3 currentPos = transform.position;
            Vector3 nextPos = currentPos + velocity * Time.deltaTime;

            // =====================================================================
            // СТОЛКНОВЕНИЯ (BoxCast + Sliding)
            // =====================================================================
            Vector3 finalPos = nextPos;

            if (_collision != null &&
                _collision.Resolve(currentPos, nextPos, out Vector3 resolvedPos))
            {
                // 1) Полный стоп — фронтальное или угловое столкновение
                if (resolvedPos == currentPos)
                {
                    currentSpeed = 0f;
                    finalPos = currentPos;
                }
                // 2) Скольжение вдоль поверхности
                else
                {
                    // НЕ сбрасываем currentSpeed,
                    // потому что это не фронтальный удар, а боковое касание.
                    finalPos = resolvedPos;
                }
            }
            else
            {
                // 3) Чистое движение без препятствий
                finalPos = nextPos;
            }

            // =====================================================================
            // ГРАНИЦЫ КАРТЫ (Clamp)
            // =====================================================================
            if (Battle.Instance != null)
            {
                Vector3 clamped = Battle.Instance.ClampPosition(finalPos);

                if (Mathf.Abs(finalPos.x - clamped.x) > 0.001f ||
                    Mathf.Abs(finalPos.z - clamped.z) > 0.001f)
                {
                    currentSpeed = 0f; // упёрлись в глобальную границу
                }

                finalPos = clamped;
            }

            // финальное перемещение
            transform.position = finalPos;
            _collision.ResolveShipOverlap();
        }
    }
}


