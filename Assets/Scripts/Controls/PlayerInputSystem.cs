using Tanks;
using UnityEngine;

public class PlayerInputSystem : MonoBehaviour, IPlayerInput
{
	private PlayerControls controls;

	private Vector2 steering;
	private float throttleAxis;

	private bool fireLMB;
	private bool fireRMB;

	public Vector2 Steering => steering;
	public float Throttle => throttleAxis;

	public bool FireLMB => fireLMB;
	public bool FireRMB => fireRMB;

	private void Awake()
	{
		controls = new PlayerControls();

		// --- движение ---
		controls.Tank.Steering.performed += ctx =>
			steering = ctx.ReadValue<Vector2>();
		controls.Tank.Steering.canceled += _ =>
			steering = Vector2.zero;

		controls.Tank.Throttle.performed += ctx =>
			throttleAxis = ctx.ReadValue<float>();
		controls.Tank.Throttle.canceled += _ =>
			throttleAxis = 0f;

		// --- стрельба ---
		controls.Tank.FireLMB.performed += _ => fireLMB = true;
		controls.Tank.FireLMB.canceled += _ => fireLMB = false;

		controls.Tank.FireRMB.performed += _ => fireRMB = true;
		controls.Tank.FireRMB.canceled += _ => fireRMB = false;
	}

	private void OnEnable() => controls.Enable();
	private void OnDisable() => controls.Disable();
}