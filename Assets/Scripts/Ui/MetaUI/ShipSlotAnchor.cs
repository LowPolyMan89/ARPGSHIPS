using UnityEngine;

namespace Ships
{
	/// <summary>
	/// Якорь слота на корпусе в мета-сцене. Содержит id грида и локальные координаты внутри него.
	/// </summary>
	[RequireComponent(typeof(Collider))]
	public class ShipSlotAnchor : MonoBehaviour
	{
		public string GridId = "weapon_left";
		public ShipGridType GridType = ShipGridType.WeaponGrid;
		public Vector2 CellPosition;
		public float RotationDeg;

		public Transform MountPoint => transform;
	}
}
