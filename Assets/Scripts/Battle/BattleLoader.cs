namespace Ships
{
	using UnityEngine;

	public class BattleLoader : MonoBehaviour
	{
		public GameObject PlayerShipPrefab;

		private void Start()
		{
			LoadTestShip();
		}

		private void LoadTestShip()
		{
			var hull = HullLoader.Load("hull_test_frigate");
			if (hull == null) return;

			var go = Instantiate(PlayerShipPrefab, Vector3.zero, Quaternion.identity);
			var ship = go.GetComponent<PlayerShip>();

			//ship.InitFromTestHull(hull);

			Debug.Log("Test ship loaded from JSON");
		}
	}

}