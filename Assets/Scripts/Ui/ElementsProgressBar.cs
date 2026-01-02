using System.Collections.Generic;
using UnityEngine;

namespace Ships
{
	//управляет элементами в Layout шруппе на этом GameObject 
	//кол-во элементов = максимальному значению
	public class ElementsProgressBar : MonoBehaviour
	{
		public GameObject ElementToExpose;
		private List<GameObject> _exposedElements = new List<GameObject>();

		public void Init(int maximumValue)
		{
			for(int i = 0; i<maximumValue; i++)
				_exposedElements.Add(GameObject.Instantiate(ElementToExpose, transform));
		}
		public float CalculateAmount(float current, float max)
		{	
			if(_exposedElements.Count == 0)
				return 0f;

			var normalized = max > 0f ? Mathf.Clamp01(current / max) : 0f;
			var activeCount = Mathf.FloorToInt(normalized * _exposedElements.Count);

			for(int i = 0; i < _exposedElements.Count; i++)
				_exposedElements[i].SetActive(i < activeCount);

			return normalized;
		}
	}
}