using UnityEngine;

namespace Ships
{
	public class BattleCamera : MonoBehaviour
	{
		[Header("2D Setup")]
		public bool AutoSetupTopDown2D = true;

		[Header("Follow Settings")]
		public float followSmooth = 0.2f;
		public float moveOffsetStrength = 2f;
		private Vector3 _velocity;
		private PlayerShip _player;

		private float _fixedY;
		private float _fixedZ;
		private Camera _camera;

		private void Awake()
		{
			_camera = GetComponent<Camera>();
			if (AutoSetupTopDown2D && _camera != null && Battle.Instance != null && Battle.Instance.Plane == Battle.WorldPlane.XY)
			{
				_camera.orthographic = true;
				transform.rotation = Quaternion.identity;
			}

			_fixedY = transform.position.y;
			_fixedZ = transform.position.z;
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
			var plane = Battle.Instance != null ? Battle.Instance.Plane : Battle.WorldPlane.XY;
			var playerPos = _player.transform.position;

			// небольшое смещение в сторону движения (без Y)
			var vel = plane == Battle.WorldPlane.XY
				? new Vector3(_player.Velocity.x, _player.Velocity.y, 0f)
				: new Vector3(_player.Velocity.x, 0f, _player.Velocity.z);

			var offset = vel.sqrMagnitude > 0.01f
				? vel.normalized * moveOffsetStrength
				: Vector3.zero;

			var targetPos = playerPos + offset;

			// возвращаем фиксированную ось
			if (plane == Battle.WorldPlane.XY)
				targetPos.z = _fixedZ;
			else
				targetPos.y = _fixedY;

			var smoothed = Vector3.SmoothDamp(
				transform.position,
				targetPos,
				ref _velocity,
				followSmooth
			);
			

			transform.position = smoothed;
		}
		
	}
}
