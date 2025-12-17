using UnityEngine;

namespace Ships
{
	[RequireComponent(typeof(ShipMovement))]
	public class PlayerShipController : MonoBehaviour
	{
		private ShipMovement _movement;
		private PlayerInputSystem _hw;
		private PlayerInputUI _ui;

		private void Awake()
		{
			_movement = GetComponent<ShipMovement>();
			_hw = FindObjectOfType<PlayerInputSystem>();
			_ui = FindObjectOfType<PlayerInputUI>();
		}

		private void Update()
		{
			var hardware = _hw ? _hw.Steering : Vector2.zero;
			var ui = _ui ? _ui.SteeringUI : Vector2.zero;

			var sum = hardware + ui;

			_movement.SetInput(sum.y, sum.x);
		}
	}
}
