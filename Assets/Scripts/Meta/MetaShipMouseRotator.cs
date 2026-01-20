using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Ships
{
	public class MetaShipMouseRotator : MonoBehaviour
	{
		[SerializeField] private Transform _target;
		[SerializeField] private float _degreesPerPixel = 0.2f;
		[SerializeField] private int _mouseButton = 0;
		[SerializeField] private bool _invert;
		[SerializeField] private bool _ignoreWhenOverUi = true;

		private bool _dragging;
		private Vector2 _lastPos;

		private void Awake()
		{
			if (_target == null)
				_target = transform;
		}

		private void Update()
		{
			if (_ignoreWhenOverUi && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
			{
				_dragging = false;
				return;
			}

			if (WasButtonPressedThisFrame())
			{
				if (TryGetMousePosition(out var pos))
				{
					_dragging = true;
					_lastPos = pos;
				}
				return;
			}

			if (WasButtonReleasedThisFrame())
			{
				_dragging = false;
				return;
			}

			if (!_dragging || !IsButtonPressed())
				return;

			if (!TryGetMousePosition(out var currentPos))
				return;

			var delta = currentPos - _lastPos;
			_lastPos = currentPos;

			if (Mathf.Approximately(delta.x, 0f))
				return;

			var sign = _invert ? -1f : 1f;
			var yaw = delta.x * _degreesPerPixel * sign;
			if (_target != null)
				_target.Rotate(Vector3.up, yaw, Space.World);
		}

		private bool WasButtonPressedThisFrame()
		{
#if ENABLE_INPUT_SYSTEM
			var mouse = Mouse.current;
			if (mouse == null)
				return false;
			return _mouseButton switch
			{
				0 => mouse.leftButton.wasPressedThisFrame,
				1 => mouse.rightButton.wasPressedThisFrame,
				2 => mouse.middleButton.wasPressedThisFrame,
				_ => mouse.leftButton.wasPressedThisFrame
			};
#elif ENABLE_LEGACY_INPUT_MANAGER
			return Input.GetMouseButtonDown(_mouseButton);
#else
			return false;
#endif
		}

		private bool WasButtonReleasedThisFrame()
		{
#if ENABLE_INPUT_SYSTEM
			var mouse = Mouse.current;
			if (mouse == null)
				return false;
			return _mouseButton switch
			{
				0 => mouse.leftButton.wasReleasedThisFrame,
				1 => mouse.rightButton.wasReleasedThisFrame,
				2 => mouse.middleButton.wasReleasedThisFrame,
				_ => mouse.leftButton.wasReleasedThisFrame
			};
#elif ENABLE_LEGACY_INPUT_MANAGER
			return Input.GetMouseButtonUp(_mouseButton);
#else
			return false;
#endif
		}

		private bool IsButtonPressed()
		{
#if ENABLE_INPUT_SYSTEM
			var mouse = Mouse.current;
			if (mouse == null)
				return false;
			return _mouseButton switch
			{
				0 => mouse.leftButton.isPressed,
				1 => mouse.rightButton.isPressed,
				2 => mouse.middleButton.isPressed,
				_ => mouse.leftButton.isPressed
			};
#elif ENABLE_LEGACY_INPUT_MANAGER
			return Input.GetMouseButton(_mouseButton);
#else
			return false;
#endif
		}

		private bool TryGetMousePosition(out Vector2 position)
		{
#if ENABLE_INPUT_SYSTEM
			var mouse = Mouse.current;
			if (mouse == null)
			{
				position = default;
				return false;
			}
			position = mouse.position.ReadValue();
			return true;
#elif ENABLE_LEGACY_INPUT_MANAGER
			position = Input.mousePosition;
			return true;
#else
			position = default;
			return false;
#endif
		}
	}
}
