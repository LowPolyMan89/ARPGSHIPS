
namespace Tanks
{
	using UnityEngine;
	using UnityEngine.UI;

	public class PlayerInputUI : MonoBehaviour
	{
		public VariableJoystick stick;
		public Slider throttleSlider;

		public Vector2 SteeringUI =>
			stick != null ? new Vector2(stick.Horizontal, stick.Vertical) : Vector2.zero;

		public float SliderValue =>
			throttleSlider != null ? throttleSlider.value : 0f;

		public void SetSlider(float value)
		{
			if (throttleSlider != null)
				throttleSlider.SetValueWithoutNotify(value);
		}
	}

}