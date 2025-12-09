using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Mime;
using UnityEngine;
using UnityEngine.UI;

namespace Tanks
{
	public class PlayerTankStatsContainerUi : MonoBehaviour
	{
		public List<ShieldUi> ShieldUis = new List<ShieldUi>();
		private ShieldController _shieldController;
		[SerializeField] private Image _hpBar;
		private static readonly int Fill = Shader.PropertyToID("_Fill");

		public void OnUpdate()
		{
			var tank = Battle.Instance.Player;
			if (tank)
			{
				if (!_shieldController)
					_shieldController = tank.GetComponent<ShieldController>();
				_hpBar.material.SetFloat(Fill, tank.TankStats.GetStat(StatType.HitPoint).Amount);
			}

			if (_shieldController)
			{
				foreach (var sector in _shieldController.Sectors)
				{
					var ui = ShieldUis.Find(x => x.Side == sector.Side);
					ui.Image.material.SetFloat(Fill, sector.ShieldHP.Amount);
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