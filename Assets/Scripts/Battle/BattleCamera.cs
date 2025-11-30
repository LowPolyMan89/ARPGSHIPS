using UnityEngine;

namespace Ships
{
	public class BattleCamera : MonoBehaviour
	{
		[Header("Follow Settings")]
		public float followSmooth = 0.2f;
		public float moveOffsetStrength = 2f;

		private Vector3 velocity;
		private Camera cam;
		private float fixedZ;

		private void Awake()
		{
			cam = Camera.main;
			fixedZ = transform.position.z;      // фиксируем Z
		}

		private void LateUpdate()
		{
			if (Battle.Instance == null || Battle.Instance.Player == null)
				return;

			FollowPlayer();
		}

		private void FollowPlayer()
		{
			PlayerShip player = Battle.Instance.Player;

			Vector2 basePos = player.transform.position;
			Vector2 offset = player.Velocity.normalized * moveOffsetStrength;
			Vector2 targetPos = basePos + offset;

			Vector3 smoothed = Vector3.SmoothDamp(
				transform.position,
				new Vector3(targetPos.x, targetPos.y, fixedZ),
				ref velocity,
				followSmooth
			);

			smoothed = ClampCameraToBounds(smoothed);
			smoothed.z = fixedZ;

			transform.position = smoothed;
		}

		private Vector3 ClampCameraToBounds(Vector3 pos)
		{
			Battle b = Battle.Instance;

			float camHeight = cam.orthographicSize;
			float camWidth = camHeight * cam.aspect;

			float minX = b.MinBounds.x + camWidth;
			float maxX = b.MaxBounds.x - camWidth;

			float minY = b.MinBounds.y + camHeight;
			float maxY = b.MaxBounds.y - camHeight;

			pos.x = Mathf.Clamp(pos.x, minX, maxX);
			pos.y = Mathf.Clamp(pos.y, minY, maxY);

			return pos;
		}
	}
}