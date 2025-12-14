using UnityEngine;

namespace Tanks
{
	[RequireComponent(typeof(TankMovement))]
	public class PlayerTankController : MonoBehaviour
	{
		private TankMovement _movement;
		private PlayerInputSystem _hw;
		private PlayerInputUI _ui;

		private void Awake()
		{
			_movement = GetComponent<TankMovement>();
			_hw = FindObjectOfType<PlayerInputSystem>();
			_ui = FindObjectOfType<PlayerInputUI>();
		}

		private void Update()
		{
			Vector2 hardware = _hw ? _hw.Steering : Vector2.zero;
			Vector2 ui = _ui ? _ui.SteeringUI : Vector2.zero;

			Vector2 sum = hardware + ui;

			_movement.SetInput(sum.y, sum.x);
		}
	}
}