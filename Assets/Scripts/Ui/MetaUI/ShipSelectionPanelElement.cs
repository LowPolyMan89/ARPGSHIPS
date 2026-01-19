using UnityEngine;
using UnityEngine.UI;

public class ShipSelectionPanelElement : MonoBehaviour
{
    [SerializeField] private Button _selectButton;
    [SerializeField] private Button _changeButton;
    [SerializeField] private Image _currentShipImage;
    [SerializeField] private bool _isFlagship;
    [SerializeField] private RectTransform _selectShipsRoot;
    [SerializeField] private GameObject _selectionShipsListPanel;
    [SerializeField] private SelectShipElementButton _shipElement;
    [SerializeField] private Sprite _nonShipSprite;
}
