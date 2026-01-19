using UnityEngine;

namespace Ships
{
	public class BattleCamera : MonoBehaviour
	{
		[Header("Move Settings")]
		public float MoveSpeed = 12f;

		[Header("Zoom Settings")]
		public float ZoomSpeed = 2.5f;
		public float ZoomMin = -8f;
		public float ZoomMax = 8f;

		[Header("Camera Setup")]
		public Transform CameraTransform;
		public Vector3 CameraOffset;

		private PlayerInputSystem _input;
		private PlayerControls _controls;
		private float _zoom;
		private Vector3 _baseLocalPos;
		private Quaternion _baseLocalRot;

		private void Awake()
		{
			_input = FindAnyObjectByType<PlayerInputSystem>();
			if (_input == null)
			{
				_controls = new PlayerControls();
				_controls.Enable();
			}

			if (CameraTransform == null)
			{
				var cam = GetComponentInChildren<Camera>();
				if (cam != null)
					CameraTransform = cam.transform;
			}

			if (CameraTransform != null)
			{
				_baseLocalPos = CameraTransform.localPosition;
				_baseLocalRot = CameraTransform.localRotation;
			}
		}

		private void LateUpdate()
		{
			if (CameraTransform == null)
				return;

			HandleMove();
			HandleZoom();
		}

		private void HandleMove()
		{
			var input = _input != null
				? _input.CameraMove
				: (_controls != null ? _controls.Camera.Move.ReadValue<Vector2>() : Vector2.zero);
			if (input.sqrMagnitude < 0.0001f)
				return;

			var forward = CameraTransform.forward;
			forward.y = 0f;
			if (forward.sqrMagnitude < 0.0001f)
				forward = Vector3.forward;
			forward.Normalize();

			var right = CameraTransform.right;
			right.y = 0f;
			if (right.sqrMagnitude < 0.0001f)
				right = Vector3.right;
			right.Normalize();

			var delta = (right * input.x + forward * input.y) * MoveSpeed * Time.deltaTime;
			transform.position += delta;
		}

		private void HandleZoom()
		{
			var scroll = _input != null
				? _input.CameraZoom
				: (_controls != null ? _controls.Camera.Zoom.ReadValue<float>() : 0f);
			if (Mathf.Abs(scroll) > 0.0001f)
				_zoom = Mathf.Clamp(_zoom + scroll * ZoomSpeed, ZoomMin, ZoomMax);

			var zoomDir = _baseLocalRot * Vector3.forward;
			CameraTransform.localPosition = _baseLocalPos + CameraOffset + zoomDir * _zoom;
		}

		private void OnDestroy()
		{
			if (_controls != null)
			{
				_controls.Dispose();
				_controls = null;
			}
		}
	}
}
