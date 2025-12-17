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
		private ShieldController _shieldController;
		[SerializeField] private Image _hpBar;
		private static readonly int Fill = Shader.PropertyToID("_Fill");

		public void OnUpdate()
		{
			var ship = Battle.Instance.Player;
			if (ship)
			{
				if (!_shieldController)
					_shieldController = ship.GetComponent<ShieldController>();
				_hpBar.material.SetFloat(Fill, ship.ShipStats.GetStat(StatType.HitPoint).Amount);
			}

			if (_shieldController)
			{
			}
		}
		
	}
	
}
