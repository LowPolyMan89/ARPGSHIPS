using System.Collections.Generic;
using UnityEngine;

namespace Tanks
{
	public class Battle : MonoBehaviour
	{
		public static Battle Instance;

		[Header("Battlefield Bounds")]
		public Vector2 MinBounds = new Vector2(-50, -50);
		public Vector2 MaxBounds = new Vector2(50, 50);
		[Header("Runtime")]
		public PlayerTank Player;
		public BattleCamera CameraController;
		public List<TankBase> AllTanks = new();
		public Transform PlayerSpawnPosition;

		private void Awake()
		{
			if(Instance)
				Destroy(gameObject);
			else
				Instance = this;
		}

		public Vector2 ClampPosition(Vector2 pos)
		{
			pos.x = Mathf.Clamp(pos.x, MinBounds.x, MaxBounds.x);
			pos.y = Mathf.Clamp(pos.y, MinBounds.y, MaxBounds.y);
			return pos;
		}

		public bool IsInside(Vector2 pos)
		{
			return pos.x >= MinBounds.x && pos.x <= MaxBounds.x &&
			       pos.y >= MinBounds.y && pos.y <= MaxBounds.y;
		}
		#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			Gizmos.color = Color.green;
			Vector2 size = MaxBounds - MinBounds;
			Gizmos.DrawWireCube((MinBounds + MaxBounds) / 2f, size);
		}
		#endif
	}
	
}