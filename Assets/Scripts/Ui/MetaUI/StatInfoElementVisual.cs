using TMPro;
using UnityEngine;

namespace Ships
{
	public class StatInfoElementVisual : MonoBehaviour
	{
		public TMP_Text StatText;

		public void SetText(string text)
		{
			StatText.text = text;
		}
		
	}
}
