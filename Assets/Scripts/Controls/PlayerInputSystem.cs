using Ships;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputSystem : MonoBehaviour, IPlayerInput
{
	private PlayerControls controls;

	private Vector2 steering;
	private float throttleAxis;

	private bool fireLMB;
	private bool fireRMB;
	private Vector2 cursorScreen;

	public Vector2 Steering => steering;
	public float Throttle => throttleAxis;

	public bool FireLMB => fireLMB;
	public bool FireRMB => fireRMB;
	public Vector2 CursorScreen => cursorScreen;

	private void Awake()
	{
		controls = new PlayerControls();

		// --- движение ---
		controls.Ship.Steering.performed += ctx =>
			steering = ctx.ReadValue<Vector2>();
		controls.Ship.Steering.canceled += _ =>
			steering = Vector2.zero;

		controls.Ship.Throttle.performed += ctx =>
			throttleAxis = ctx.ReadValue<float>();
		controls.Ship.Throttle.canceled += _ =>
			throttleAxis = 0f;

		// --- стрельба ---
		controls.Ship.FireLMB.performed += _ =>
		{
			fireLMB = true;
			GameEvent.FireLmbInput(true);
		};
		controls.Ship.FireLMB.canceled += _ =>
		{
			fireLMB = false;
			GameEvent.FireLmbInput(false);
		};

		controls.Ship.FireRMB.performed += _ => fireRMB = true;
		controls.Ship.FireRMB.canceled += _ => fireRMB = false;
	}

	private void OnEnable() => controls.Enable();
	private void OnDisable() => controls.Disable();

	private void Update()
	{
		var mouse = Mouse.current;
		if (mouse == null)
			return;

		cursorScreen = mouse.position.ReadValue();
		GameEvent.CursorInput(cursorScreen);
	}
}

