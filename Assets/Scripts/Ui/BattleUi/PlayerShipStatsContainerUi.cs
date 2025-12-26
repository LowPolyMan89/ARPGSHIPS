using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Mime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ships
{
	public class PlayerShipStatsContainerUi : MonoBehaviour
	{
		private ShieldController _shieldController;
		[SerializeField] private Image _hpBar;
		[SerializeField] private Image _shieldBar;
		[SerializeField] private TMP_Text _hpText;
		[SerializeField] private TMP_Text _shieldText;
		private static readonly int Fill = Shader.PropertyToID("_Fill");

		public void OnUpdate()
		{
			var ship = Battle.Instance.Player;
			if (ship)
			{
				if (!_shieldController)
					_shieldController = ship.GetComponent<ShieldController>();
				_hpBar.material.SetFloat(Fill, ship.ShipStats.GetStat(StatType.HitPoint).Amount);
				_hpText.text = $"{ship.ShipStats.GetStat(StatType.HitPoint).Current.ToString("00")}/{ship.ShipStats.GetStat(StatType.HitPoint).Maximum.ToString("00")}";
				_shieldText.text = $"{_shieldController.ShipShield.ShieldHP.Current.ToString("00")}/{_shieldController.ShipShield.ShieldHP.Maximum.ToString("00")}";
			}

			if (_shieldController)
			{
				_shieldBar.material.SetFloat(Fill, _shieldController.ShipShield.ShieldHP.Amount);
			}
		}
		
	}
	
}
