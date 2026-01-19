using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Ships
{
	public sealed class BattleSelectionController : MonoBehaviour
	{
		[SerializeField] private Camera _camera;
		[SerializeField] private float _clickThreshold = 6f;
		[SerializeField] private Color _rectFill = new Color(0.2f, 0.8f, 1f, 0.15f);
		[SerializeField] private Color _rectBorder = new Color(0.2f, 0.8f, 1f, 0.9f);
		[SerializeField] private float _rectBorderWidth = 2f;

		private PlayerInputSystem _input;
		private bool _prevLmb;
		private Vector2 _dragStart;
		private Vector2 _dragCurrent;
		private bool _dragging;
		private bool _dragRectActive;

		private void Awake()
		{
			_input = FindAnyObjectByType<PlayerInputSystem>();
			if (_camera == null)
				_camera = Camera.main;
		}

		private void Start()
		{
			EnsureDefaultSelection();
		}

		private void Update()
		{
			if (_input == null)
				return;

			var lmb = _input.FireLMB;
			if (lmb && !_prevLmb)
				BeginSelection();
			else if (!lmb && _prevLmb)
				EndSelection();
			else if (lmb && _dragging)
				UpdateDragState();

			_prevLmb = lmb;
		}

		private void BeginSelection()
		{
			if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
				return;

			_dragStart = _input.CursorScreen;
			_dragCurrent = _dragStart;
			_dragging = true;
			_dragRectActive = false;
		}

		private void UpdateDragState()
		{
			if (!_dragging)
				return;

			_dragCurrent = _input.CursorScreen;
			var delta = _dragCurrent - _dragStart;
			if (delta.sqrMagnitude >= _clickThreshold * _clickThreshold)
				_dragRectActive = true;
		}

		private void EndSelection()
		{
			if (!_dragging)
				return;

			_dragging = false;
			_dragRectActive = false;

			if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
				return;

			var end = _input.CursorScreen;
			var delta = end - _dragStart;
			if (delta.sqrMagnitude <= _clickThreshold * _clickThreshold)
				SelectByClick(end);
			else
				SelectByRect(_dragStart, end);
		}

		private void SelectByClick(Vector2 screenPos)
		{
			if (_camera == null)
				_camera = Camera.main;
			if (_camera == null || Battle.Instance == null)
				return;

			var ray = _camera.ScreenPointToRay(screenPos);
			if (Physics.Raycast(ray, out var hit))
			{
				var ship = hit.collider != null ? hit.collider.GetComponentInParent<ShipBase>() : null;
				if (ship != null && ship.SideType == SideType.Player && ship.IsAlive)
				{
					Battle.Instance.SetSelection(new List<ShipBase> { ship });
					return;
				}
			}

			Battle.Instance.SetSelection(new List<ShipBase>());
		}

		private void SelectByRect(Vector2 start, Vector2 end)
		{
			if (Battle.Instance == null)
				return;

			var rect = GetScreenRect(start, end);
			var selected = new List<ShipBase>();

			for (var i = 0; i < Battle.Instance.AllShips.Count; i++)
			{
				var ship = Battle.Instance.AllShips[i];
				if (ship == null || ship.SideType != SideType.Player || !ship.IsAlive)
					continue;

				var sp = WorldToScreen(ship.transform.position);
				if (sp.z < 0f)
					continue;

				if (rect.Contains(sp))
					selected.Add(ship);
			}

			Battle.Instance.SetSelection(selected);
		}

		private void OnGUI()
		{
			if (!_dragging || !_dragRectActive)
				return;

			var rect = GetGuiRect(_dragStart, _dragCurrent);
			GUI.color = _rectFill;
			GUI.DrawTexture(rect, Texture2D.whiteTexture);
			DrawGuiBorder(rect, _rectBorderWidth, _rectBorder);
			GUI.color = Color.white;
		}

		private static Rect GetGuiRect(Vector2 start, Vector2 end)
		{
			var min = Vector2.Min(start, end);
			var max = Vector2.Max(start, end);
			return Rect.MinMaxRect(min.x, Screen.height - max.y, max.x, Screen.height - min.y);
		}

		private static void DrawGuiBorder(Rect rect, float thickness, Color color)
		{
			var t = Mathf.Max(1f, thickness);
			GUI.color = color;
			GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, t), Texture2D.whiteTexture);
			GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - t, rect.width, t), Texture2D.whiteTexture);
			GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, t, rect.height), Texture2D.whiteTexture);
			GUI.DrawTexture(new Rect(rect.xMax - t, rect.yMin, t, rect.height), Texture2D.whiteTexture);
		}

		private Vector3 WorldToScreen(Vector3 worldPos)
		{
			if (_camera == null)
				_camera = Camera.main;
			return _camera != null ? _camera.WorldToScreenPoint(worldPos) : Vector3.zero;
		}

		private static Rect GetScreenRect(Vector2 a, Vector2 b)
		{
			var min = Vector2.Min(a, b);
			var max = Vector2.Max(a, b);
			return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
		}

		private void EnsureDefaultSelection()
		{
			if (Battle.Instance == null || Battle.Instance.SelectedShips.Count > 0)
				return;

			for (var i = 0; i < Battle.Instance.AllShips.Count; i++)
			{
				var ship = Battle.Instance.AllShips[i];
				if (ship == null || ship.SideType != SideType.Player || !ship.IsAlive)
					continue;

				Battle.Instance.SetSelection(new List<ShipBase> { ship });
				break;
			}
		}
	}
}
