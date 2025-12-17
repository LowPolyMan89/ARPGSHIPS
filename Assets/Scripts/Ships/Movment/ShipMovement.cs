using UnityEngine;

namespace Ships
{
    public class ShipMovement : MonoBehaviour
    {
        public enum MovementMode
        {
            Free3D = 0,
            Rails2D = 1,
            TopDown2D = 2
        }

        [Header("Runtime Debug")]
        public float currentSpeed;      // текущая линейная скорость
        public float currentTurnSpeed;  // скорость поворота корпуса
        public Vector3 velocity;        // вектор движения

        [Header("Movement Mode")]
        public MovementMode Mode = MovementMode.TopDown2D;

        [Tooltip("Направление рельсы в выбранной плоскости (XY или XZ).")]
        public Vector2 railDirection = Vector2.up;

        [Tooltip("Если включено, корабль едет вперёд по рельсе даже без газа (inputForward).")]
        public bool autoForward = true;

        [Tooltip("Глобальный множитель скорости по рельсе.")]
        public float railSpeedMultiplier = 1f;

        [Tooltip("Глобальный множитель боковой скорости (strafe).")]
        public float strafeSpeedMultiplier = 1f;

        private ShipBase _ship;
        private PlayerInputSystem _hw;
        private PlayerInputUI _ui;
        private ShipCollisionKinematic _collision;

        private float _inputForward;    // W/S (-1..1)
        private float _inputTurn;       // A/D (-1..1)

        private float _fixedAxis;       // фиксируем Z (для XY) или Y (для XZ)

        [Header("TopDown 2D Behaviour")]
        [SerializeField] private float velocityAlignRate = 8f;

        private Vector2 _velocity2D;
        private float _speedSigned2D;

        private void Awake()
        {
            _ship = GetComponent<ShipBase>();
            _collision = GetComponent<ShipCollisionKinematic>();

            _hw = FindObjectOfType<PlayerInputSystem>();
            _ui = FindObjectOfType<PlayerInputUI>();

            var plane = Battle.Instance != null ? Battle.Instance.Plane : Battle.WorldPlane.XY;
            _fixedAxis = plane == Battle.WorldPlane.XY ? transform.position.z : transform.position.y;
        }

        private void Update()
        {
            if (Mode == MovementMode.TopDown2D)
            {
                HandleTopDown2DMovement();
            }
            else if (Mode == MovementMode.Rails2D)
            {
                HandleRails2DMovement();
            }
            else
            {
                HandleRotation();
                HandleMovement();
            }
        }
        public void SetInput(float forward, float turn)
        {
            _inputForward = Mathf.Clamp(forward, -1f, 1f);
            _inputTurn = Mathf.Clamp(turn, -1f, 1f);
        }

        private void HandleRails2DMovement()
        {
            var plane = Battle.Instance != null ? Battle.Instance.Plane : Battle.WorldPlane.XY;

            var accel = _ship.GetStat(StatType.Acceleration).Current;
            var brake = _ship.GetStat(StatType.BrakePower).Current;
            var forwardMax = _ship.GetStat(StatType.MoveSpeed).Current * railSpeedMultiplier;

            var forwardInput = _inputForward;
            if (autoForward && Mathf.Abs(forwardInput) < 0.01f)
                forwardInput = 1f;

            var targetSpeed = Mathf.Max(0f, forwardMax * forwardInput);

            currentSpeed = Mathf.MoveTowards(
                currentSpeed,
                targetSpeed,
                (targetSpeed > currentSpeed ? accel : brake) * Time.deltaTime
            );

            var strafeStat = _ship.GetStat(StatType.TurnSpeed).Current;
            var strafeSpeed = strafeStat * _inputTurn * strafeSpeedMultiplier;

            var railDir2 = railDirection.sqrMagnitude > 0.0001f ? railDirection.normalized : Vector2.up;

            Vector3 railDir3;
            Vector3 strafeDir3;

            if (plane == Battle.WorldPlane.XY)
            {
                railDir3 = new Vector3(railDir2.x, railDir2.y, 0f);
                strafeDir3 = new Vector3(-railDir2.y, railDir2.x, 0f);

                var desiredRot = Quaternion.LookRotation(Vector3.forward, railDir3);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    desiredRot,
                    strafeStat * 2f * Time.deltaTime
                );
            }
            else
            {
                railDir3 = new Vector3(railDir2.x, 0f, railDir2.y);
                strafeDir3 = new Vector3(-railDir2.y, 0f, railDir2.x);
            }

            velocity = railDir3 * currentSpeed + strafeDir3 * strafeSpeed;

            var currentPos = transform.position;
            var nextPos = currentPos + velocity * Time.deltaTime;

            if (plane == Battle.WorldPlane.XY)
                nextPos.z = _fixedAxis;
            else
                nextPos.y = _fixedAxis;

            var finalPos = nextPos;

            if (Battle.Instance != null)
            {
                finalPos = Battle.Instance.ClampPosition(finalPos);

                if (plane == Battle.WorldPlane.XY)
                    finalPos.z = _fixedAxis;
                else
                    finalPos.y = _fixedAxis;
            }

            transform.position = finalPos;
        }

        private void HandleTopDown2DMovement()
        {
            var plane = Battle.Instance != null ? Battle.Instance.Plane : Battle.WorldPlane.XY;

            // Only supported plane for this mode
            if (plane != Battle.WorldPlane.XY)
                plane = Battle.WorldPlane.XY;

            // ============================
            // ROTATION: A/D поворачивает корпус (ось Z)
            // ============================
            var turnStat = _ship.GetStat(StatType.TurnSpeed).Current; // deg/sec
            var turnSpeed = turnStat * _inputTurn;
            if (Mathf.Abs(turnSpeed) > 0.001f)
            {
                transform.rotation *= Quaternion.Euler(0f, 0f, -turnSpeed * Time.deltaTime);
            }

            // ============================
            // THRUST: W/S задаёт тягу вперёд/назад
            // ============================
            var accel = _ship.GetStat(StatType.Acceleration).Current;
            var brakePower = _ship.GetStat(StatType.BrakePower).Current;
            var forwardMax = _ship.GetStat(StatType.MoveSpeed).Current;
            var reverseMax = _ship.GetStat(StatType.MoveSpeedRear).Current;

            var targetSpeed = 0f;
            if (_inputForward > 0.01f)
                targetSpeed = forwardMax * _inputForward;
            else if (_inputForward < -0.01f)
                targetSpeed = reverseMax * _inputForward; // inputForward отрицательный => targetSpeed отрицательный

            _speedSigned2D = Mathf.MoveTowards(
                _speedSigned2D,
                targetSpeed,
                (Mathf.Abs(targetSpeed) > Mathf.Abs(_speedSigned2D) ? accel : brakePower) * Time.deltaTime
            );

            // ============================
            // INERTIA: velocity стремится к направлению корпуса (дуга)
            // ============================
            var forward = (Vector2)transform.up;
            var desiredDir = _speedSigned2D >= 0f ? forward : -forward;
            var absSpeed = Mathf.Abs(_speedSigned2D);

            if (_velocity2D.sqrMagnitude > 0.001f && absSpeed > 0.001f)
            {
                var newDir = Vector2.Lerp(
                    _velocity2D.normalized,
                    desiredDir,
                    velocityAlignRate * Time.deltaTime
                ).normalized;

                _velocity2D = newDir * absSpeed;
            }
            else
            {
                _velocity2D = desiredDir * absSpeed;
            }

            currentSpeed = _speedSigned2D;
            velocity = new Vector3(_velocity2D.x, _velocity2D.y, 0f);

            var currentPos = transform.position;
            var nextPos = currentPos + (Vector3)_velocity2D * Time.deltaTime;

            if (plane == Battle.WorldPlane.XY)
                nextPos.z = _fixedAxis;
            else
                nextPos.y = _fixedAxis;

            var finalPos = nextPos;
            if (Battle.Instance != null)
            {
                var unclamped = finalPos;
                var clamped = Battle.Instance.ClampPosition(unclamped);

                if (unclamped.x != clamped.x || unclamped.y != clamped.y)
				{
                    _velocity2D = Vector2.zero;
					_speedSigned2D = 0f;
				}

                finalPos = clamped;

                if (plane == Battle.WorldPlane.XY)
                    finalPos.z = _fixedAxis;
                else
                    finalPos.y = _fixedAxis;
            }

            transform.position = finalPos;
        }
        // =====================================================================
        // ROTATION
        // =====================================================================
        private void HandleRotation()
        {
            var turnStat = _ship.GetStat(StatType.TurnSpeed).Current;
            var turnSpeed = turnStat * _inputTurn;

            if (Mathf.Abs(turnSpeed) < 0.001f)
                return;

            var nextRot = transform.rotation * Quaternion.Euler(0f, turnSpeed * Time.deltaTime, 0f);

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
            var accel = _ship.GetStat(StatType.Acceleration).Current;
            var brake = _ship.GetStat(StatType.BrakePower).Current;
            var forwardMax = _ship.GetStat(StatType.MoveSpeed).Current;
            var reverseMax = _ship.GetStat(StatType.MoveSpeedRear).Current;

            var targetSpeed = 0f;

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
            var currentPos = transform.position;
            var nextPos = currentPos + velocity * Time.deltaTime;

            // =====================================================================
            // СТОЛКНОВЕНИЯ (BoxCast + Sliding)
            // =====================================================================
            var finalPos = nextPos;

            if (_collision != null &&
                _collision.Resolve(currentPos, nextPos, out var resolvedPos))
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
                var clamped = Battle.Instance.ClampPosition(finalPos);

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
