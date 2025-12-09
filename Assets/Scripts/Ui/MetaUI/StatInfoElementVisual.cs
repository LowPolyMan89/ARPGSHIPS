using TMPro;
using UnityEngine;

namespace Tanks
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