using UnityEngine;

namespace Tanks
{
	public class SafeArea : MonoBehaviour
	{
		private RectTransform _rectTransform;
		private Rect _lastSafeArea = new Rect(0, 0, 0, 0);
		private ScreenOrientation _lastOrientation;

		private void Awake()
		{
			_rectTransform = GetComponent<RectTransform>();
			ApplySafeArea();
		}

		private void Update()
		{
			if (_lastSafeArea != Screen.safeArea || _lastOrientation != Screen.orientation)
			{
				ApplySafeArea();
			}
		}

		private void ApplySafeArea()
		{
			var safe = Screen.safeArea;

			var anchorMin = safe.position;
			var anchorMax = safe.position + safe.size;

			anchorMin.x /= Screen.width;
			anchorMin.y /= Screen.height;
			anchorMax.x /= Screen.width;
			anchorMax.y /= Screen.height;

			_rectTransform.anchorMin = anchorMin;
			_rectTransform.anchorMax = anchorMax;

			_lastSafeArea = safe;
			_lastOrientation = Screen.orientation;
		}
	}
}