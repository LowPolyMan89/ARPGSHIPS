using UnityEngine;
using System.Collections.Generic;

namespace Tanks
{
	public class Battle : MonoBehaviour
	{
		public static Battle Instance;

		[Header("Battlefield Bounds (3D)")]
		public Vector3 MinBounds = new Vector3(-500, 0, -500);
		public Vector3 MaxBounds = new Vector3(500, 10, 500);

		[Header("Runtime")]
		public PlayerTank Player;
		public BattleCamera CameraController;
		public List<TankBase> AllTanks = new();
		public Transform PlayerSpawnPosition;

		private void Awake()
		{
			if (Instance)
				Destroy(gameObject);
			else
				Instance = this;
		}

		/// <summary>
		/// Ограничивает позицию в 3D-объёме.
		/// </summary>
		public Vector3 ClampPosition(Vector3 pos)
		{
			return new Vector3(
				Mathf.Clamp(pos.x, MinBounds.x, MaxBounds.x),
				Mathf.Clamp(pos.y, MinBounds.y, MaxBounds.y),
				Mathf.Clamp(pos.z, MinBounds.z, MaxBounds.z)
			);
		}

		/// <summary>
		/// Проверяет, находится ли точка внутри объёма.
		/// </summary>
		public bool IsInside(Vector3 pos)
		{
			return pos.x >= MinBounds.x && pos.x <= MaxBounds.x &&
			       pos.y >= MinBounds.y && pos.y <= MaxBounds.y &&
			       pos.z >= MinBounds.z && pos.z <= MaxBounds.z;
		}

#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			Gizmos.color = Color.green;

			Vector3 center = (MinBounds + MaxBounds) * 0.5f;
			Vector3 size = MaxBounds - MinBounds;

			Gizmos.DrawWireCube(center, size);
		}
#endif
	}
}