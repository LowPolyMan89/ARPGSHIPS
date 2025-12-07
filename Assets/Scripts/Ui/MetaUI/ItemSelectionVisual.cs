using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ships
{
	public class ItemSelectionVisual : MonoBehaviour
	{
		[SerializeField] private StatInfoElementVisual _mainStatElementVisualPrefab;
		[SerializeField] private StatInfoElementVisual _effectStatElementVisualPrefab;
		[SerializeField] private ItemVisualInfoPanel _selectedItemPanel;
		[SerializeField] private ItemVisualInfoPanel _compareItemPanel;

		private const float OffsetX = 20f;
		private const float OffsetY = -20f;

		// ⏳ задержка тултипа
		[SerializeField] private float _tooltipDelay = 0.4f;

		private Coroutine _tooltipRoutine;

		private void Start()
		{
			_selectedItemPanel.PanelGameObject.SetActive(false);
			_compareItemPanel.PanelGameObject.SetActive(false);
		}

		// 🔥 вызывается из инвентаря при наведении
		public void Show(InventoryItem item, PointerEventData pointerEventData)
		{
			// если уже была корутина — сбиваем
			if (_tooltipRoutine != null)
				StopCoroutine(_tooltipRoutine);

			_tooltipRoutine = StartCoroutine(ShowWithDelay(item, pointerEventData));
		}

		public void Hide()
		{
			// отменяем задержку, если была
			if (_tooltipRoutine != null)
			{
				StopCoroutine(_tooltipRoutine);
				_tooltipRoutine = null;
			}

			_selectedItemPanel.PanelGameObject.SetActive(false);
			_compareItemPanel.PanelGameObject.SetActive(false);
		}

		private IEnumerator ShowWithDelay(InventoryItem item, PointerEventData eventData)
		{
			yield return new WaitForSeconds(_tooltipDelay);

			// если мышь за это время ушла — тултип не нужен
			if (!IsPointerStillOver(eventData))
				yield break;

			_selectedItemPanel.PanelGameObject.SetActive(true);

			PositionTooltip(eventData.position, _selectedItemPanel.PanelRect);

			_tooltipRoutine = null;
		}

		// 🔍 проверяем, что курсор всё ещё над объектом
		private bool IsPointerStillOver(PointerEventData data)
		{
			return data.pointerEnter != null;
		}

		// -------------------------------------------
		// твой метод позиции
		// -------------------------------------------

		public void PositionTooltip(Vector2 screenPos, RectTransform tooltip)
		{
			Canvas canvas = tooltip.GetComponentInParent<Canvas>();
			RectTransform canvasRect = canvas.GetComponent<RectTransform>();

			Vector2 localPoint;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				canvasRect,
				screenPos,
				canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
				out localPoint
			);

			localPoint += new Vector2(OffsetX, OffsetY);

			Vector2 tooltipSize = tooltip.sizeDelta;
			Vector2 canvasSize = canvasRect.rect.size;

			float minX = -canvasSize.x * 0.5f;
			float maxX = canvasSize.x * 0.5f - tooltipSize.x;

			float minY = -canvasSize.y * 0.5f;
			float maxY = canvasSize.y * 0.5f - tooltipSize.y;

			localPoint.x = Mathf.Clamp(localPoint.x, minX, maxX);
			localPoint.y = Mathf.Clamp(localPoint.y, minY, maxY);

			tooltip.position = canvasRect.TransformPoint(localPoint);
		}
	}

	[System.Serializable]
	public class ItemVisualInfoPanel
	{
		public GameObject PanelGameObject;
		public Image ItemImage;
		public RectTransform MainStatRoot;
		public RectTransform EffectStatRoot;
		public Button EquipButton;
		public Button UnEquipButton;
		public RectTransform PanelRect;
	}
}