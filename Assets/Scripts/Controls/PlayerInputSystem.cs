using Tanks;
using UnityEngine;

public class PlayerInputSystem : MonoBehaviour, IPlayerInput
{
	private PlayerControls controls;

	private Vector2 steering;
	private float throttleAxis;

	public Vector2 Steering => steering;      // -1..1 per axis
	public float Throttle => throttleAxis;    // -1..1 (Shift/Ctrl)

	private void Awake()
	{
		controls = new PlayerControls();

		controls.Tank.Steering.performed += ctx =>
			steering = ctx.ReadValue<Vector2>();
		controls.Tank.Steering.canceled += _ =>
			steering = Vector2.zero;

		controls.Tank.Throttle.performed += ctx =>
			throttleAxis = ctx.ReadValue<float>();
		controls.Tank.Throttle.canceled += _ =>
			throttleAxis = 0f;
	}

	private void OnEnable() => controls.Enable();
	private void OnDisable() => controls.Disable();
}