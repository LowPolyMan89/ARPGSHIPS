using UnityEngine;

namespace Tanks
{
    public class PlayerTankMovement : MonoBehaviour
    {
        [Header("Runtime")]
        public float currentSpeed;      // текущая линейная скорость
        public float currentTurnSpeed;  // скорость поворота корпуса
        public Vector3 velocity;

        private TankBase _tank;
        private PlayerInputSystem _hw;
        private PlayerInputUI _ui;

        // параметры управления
        private float _inputForward;    // W/S   (-1..1)
        private float _inputTurn;       // A/D   (-1..1)

        private void Awake()
        {
            _tank = GetComponent<TankBase>();
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
            Vector2 hardware = _hw != null ? _hw.Steering : Vector2.zero;
            Vector2 ui = _ui != null ? _ui.SteeringUI : Vector2.zero;

            Vector2 sum = hardware + ui;

            // sum.x = A/D   — поворот
            // sum.y = W/S   — движение
            _inputTurn = Mathf.Clamp(sum.x, -1f, 1f);
            _inputForward = Mathf.Clamp(sum.y, -1f, 1f);
        }

        // ============================================================
        // ROTATION (A/D)
        // ============================================================
        private void HandleRotation()
        {
            float turnStat = _tank.GetStat(StatType.TurnSpeed).Current;
            float turnSpeed = turnStat * _inputTurn;

            // вращаем корпус вокруг Y
            transform.Rotate(0f, turnSpeed * Time.deltaTime, 0f);
        }

        // ============================================================
        // MOVEMENT (W/S)
        // ============================================================
        private void HandleMovement()
        {
            float accel = _tank.GetStat(StatType.Acceleration).Current;
            float brake = _tank.GetStat(StatType.BrakePower).Current;
            float forwardMax = _tank.GetStat(StatType.MoveSpeed).Current;
            float reverseMax = _tank.GetStat(StatType.MoveSpeedRear).Current; // твой новый MoveSpeedRear

            float targetSpeed = 0f;

            if (_inputForward > 0.01f)
            {
                // движение вперёд
                targetSpeed = forwardMax * _inputForward;
            }
            else if (_inputForward < -0.01f)
            {
                // движение назад (ограничено MoveSpeedRear)
                targetSpeed = reverseMax * _inputForward;
            }
            else
            {
                // отпустили W/S → торможение
                targetSpeed = 0f;
            }

            // разгон или торможение
            currentSpeed = Mathf.MoveTowards(
                currentSpeed,
                targetSpeed,
                (Mathf.Abs(targetSpeed) > Mathf.Abs(currentSpeed) ? accel : brake) * Time.deltaTime
            );

            // направление — строго forward
            velocity = transform.forward * currentSpeed;

            // движение
            transform.position += velocity * Time.deltaTime;

            // ограничение карты
            if (Battle.Instance != null)
            {
                Vector3 pos = transform.position;

                Vector2 clamped = Battle.Instance.ClampPosition(new Vector2(pos.x, pos.z));
                if (Mathf.Abs(pos.x - clamped.x) > 0.001f || Mathf.Abs(pos.z - clamped.y) > 0.001f)
                {
                    currentSpeed = 0f; // обнулить скорость при ударе об границу
                }

                transform.position = new Vector3(clamped.x, pos.y, clamped.y);
            }
        }
    }
}
