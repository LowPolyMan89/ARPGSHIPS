using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Mime;
using UnityEngine;
using UnityEngine.UI;

namespace Ships
{
	public class PlayerShipStatsContainerUi : MonoBehaviour
	{
		public List<ShieldUi> ShieldUis = new List<ShieldUi>();
		private ShieldController _shieldController;
		public void OnUpdate()
		{
			var ship = Battle.Instance.Player;
			if (ship)
			{
				if (!_shieldController)
					_shieldController = ship.GetComponent<ShieldController>();
			}

			if (_shieldController)
			{
				foreach (var sector in _shieldController.Sectors)
				{
					var ui = ShieldUis.Find(x => x.Side == sector.Side);
					ui.Image.material.SetFloat("_Fill", sector.ShieldHP.Amount);
				}
			}
		}
		
		[System.Serializable]
		public class ShieldUi
		{
			public ShieldSide Side;
			public Image Image;
		}
	}
	
}