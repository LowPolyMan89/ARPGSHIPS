using System.Collections.Generic;
using Ships;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ShipSelectionPanelElement : MonoBehaviour
{
	[SerializeField] private Button _selectButton;
	[SerializeField] private Button _changeButton;
	[SerializeField] private Button _removeButton;
	[SerializeField] private Image _currentShipImage;
	[SerializeField] private bool _isFlagship;
	[SerializeField] private RectTransform _selectShipsRoot;
	[SerializeField] private GameObject _selectionShipsListPanel;
	[SerializeField] private SelectShipElementButton _shipElement;
	[SerializeField] private Sprite _nonShipSprite;

	private ShipSelectionPanel _panel;
	private int _slotIndex;
	private readonly List<SelectShipElementButton> _spawned = new();

	public void Init(ShipSelectionPanel panel, int slotIndex)
	{
		_panel = panel;
		_slotIndex = slotIndex;

		if (_selectButton != null)
		{
			_selectButton.onClick.RemoveAllListeners();
			_selectButton.onClick.AddListener(OnSelect);
		}

		if (_changeButton != null)
		{
			_changeButton.onClick.RemoveAllListeners();
			_changeButton.onClick.AddListener(OnChange);
		}

		if (_removeButton != null)
		{
			_removeButton.onClick.RemoveAllListeners();
			_removeButton.onClick.AddListener(OnRemove);
			_removeButton.interactable = !_isFlagship;
		}

		if (_selectionShipsListPanel != null)
			_selectionShipsListPanel.SetActive(false);

		Refresh();
	}

	public void Refresh()
	{
		if (_panel == null)
			return;

		var shipId = _panel.GetSlotShipId(_slotIndex);
		var icon = _panel.LoadShipIcon(shipId);
		if (_currentShipImage != null)
		{
			_currentShipImage.sprite = icon != null ? icon : _nonShipSprite;
			_currentShipImage.enabled = _currentShipImage.sprite != null;
		}
	}

	private void OnSelect()
	{
		if (_panel == null)
			return;

		_panel.SelectSlot(_slotIndex);
	}

	private void OnChange()
	{
		if (_panel == null)
			return;

		PopulateSelectionList();
	}

	private void OnRemove()
	{
		if (_panel == null || _isFlagship)
			return;

		_panel.ClearSlot(_slotIndex);
	}

	private void PopulateSelectionList()
	{
		if (_selectionShipsListPanel != null)
			_selectionShipsListPanel.SetActive(true);

		ClearSelectionList();

		if (_panel == null || _shipElement == null || _selectShipsRoot == null)
			return;

		var available = _panel.GetAvailableShipIds(_slotIndex, _isFlagship);
		for (var i = 0; i < available.Count; i++)
		{
			var shipId = available[i];
			var element = Instantiate(_shipElement, _selectShipsRoot);
			_spawned.Add(element);
			var icon = _panel.LoadShipIcon(shipId);
			element.Init(icon != null ? icon : _nonShipSprite, () =>
			{
				if (_panel.SetSlotShipId(_slotIndex, shipId))
				{
					if (_selectionShipsListPanel != null)
						_selectionShipsListPanel.SetActive(false);
					ClearSelectionList();
				}
			});
		}
	}

	private void ClearSelectionList()
	{
		for (var i = 0; i < _spawned.Count; i++)
		{
			var element = _spawned[i];
			if (element == null)
				continue;

			if (Application.isPlaying)
				Destroy(element.gameObject);
			else
				DestroyImmediate(element.gameObject);
		}

		_spawned.Clear();
	}

	private void Update()
	{
		if (_selectionShipsListPanel == null || !_selectionShipsListPanel.activeSelf)
			return;

		var mouse = Mouse.current;
		if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
			return;

		if (RectTransformUtility.RectangleContainsScreenPoint(
			    _selectionShipsListPanel.transform as RectTransform,
			    mouse.position.ReadValue(),
			    null))
			return;

		_selectionShipsListPanel.SetActive(false);
		ClearSelectionList();
	}
}
