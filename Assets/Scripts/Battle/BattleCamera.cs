using UnityEngine;

namespace Tanks
{
	public class BattleCamera : MonoBehaviour
	{
		[Header("Follow Settings")]
		public float followSmooth = 0.2f;
		public float moveOffsetStrength = 2f;

		private Vector3 _velocity;
		private PlayerTank _player;

		private float _fixedY;   // высота камеры

		private void Awake()
		{
			_fixedY = transform.position.y;  // фиксируем высоту
		}

		private void LateUpdate()
		{
			if (Battle.Instance == null || Battle.Instance.Player == null)
				return;

			if (_player == null)
				_player = Battle.Instance.Player;

			FollowPlayer();
		}

		private void FollowPlayer()
		{
			Vector3 playerPos = _player.transform.position;

			// небольшое смещение в сторону движения (без Y)
			Vector3 vel = new Vector3(_player.Velocity.x, 0, _player.Velocity.z);
			Vector3 offset = vel.sqrMagnitude > 0.01f
				? vel.normalized * moveOffsetStrength
				: Vector3.zero;

			Vector3 targetPos = playerPos + offset;

			// возвращаем нашу фиксированную высоту
			targetPos.y = _fixedY;

			Vector3 smoothed = Vector3.SmoothDamp(
				transform.position,
				targetPos,
				ref _velocity,
				followSmooth
			);

			// ограничение карты → XZ
			smoothed = ClampCameraToBounds(smoothed);

			transform.position = smoothed;
		}

		private Vector3 ClampCameraToBounds(Vector3 camPos)
		{
			Battle b = Battle.Instance;

			// твоя карта работает в X/Z, а высота это Y
			Vector2 clamped = b.ClampPosition(new Vector2(camPos.x, camPos.z));

			camPos.x = clamped.x;
			camPos.z = clamped.y;
			camPos.y = _fixedY;

			return camPos;
		}
	}
}