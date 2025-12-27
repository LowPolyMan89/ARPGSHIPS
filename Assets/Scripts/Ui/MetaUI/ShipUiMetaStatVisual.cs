using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ships
{
	public class ShipUiMetaStatVisual : MonoBehaviour
	{
		[SerializeField] private TMP_Text _valueText;
		[SerializeField] private Slider _valueSlider;
		[SerializeField] private Image _sliderColorImage;
		[SerializeField] private Stat _stat;
		public Color textColor, sliderColor;

		private void Update()
		{
			if(_stat == null)
				return;
			SetText($"{_stat.Current} / {_stat.Maximum}");
			SetSlider(_stat.Amount);
		}

		public void InitFromStat(Stat stat, Color tColor, Color sColor)
		{
			textColor = tColor;
			sliderColor = sColor;
			_stat = stat;
		}
		
		public void SetText(string text)
		{
			if (!_valueText)
				return;
			_valueText.text = text;
			_valueText.color = textColor;
		}

		public void SetSlider(float value)
		{
			if (!_valueSlider)
				return;
			_valueSlider.value = value;
			_sliderColorImage.color = sliderColor;
		}
	}
}