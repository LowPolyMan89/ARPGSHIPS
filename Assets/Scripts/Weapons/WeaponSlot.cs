using UnityEngine;

namespace Tanks
{
    public class WeaponSlot : MonoBehaviour
    {
        [Header("Slot config")]
        public WeaponSize SlotSize;
        public TankBase Owner;
        public Transform MountPoint;
        [Tooltip("Максимальный угол отклонения от слота FORWARD (для логики, не для вращения!)")]
        [SerializeField] private float _allowedAngle = 180f;
        public float AllowedAngle => _allowedAngle;

        [Header("Runtime")]
        public WeaponBase MountedWeapon;

        private void Awake()
        {
            if (!Owner)
                Owner = GetComponentInParent<TankBase>();
            MountedWeapon = WeaponBuilder.Build("item_1765113613959_pya1", this);
            if (MountedWeapon)
                InitWeapon(MountedWeapon);
        }

        /// <summary>
        /// Временная заглушка — создание статы пушки.
        /// Потом будет заменено на загрузку из JSON.
        /// </summary>
        private void InitWeapon(WeaponBase weapon)
        {
            weapon.Slot = this;
            weapon.Owner = Owner;
            
        }

        public void AttachWeapon(WeaponBase weapon)
        {
            MountedWeapon = weapon;
            if (!weapon) return;

            weapon.transform.SetParent(transform, false);

            InitWeapon(weapon);
        }

        public void DetachWeapon()
        {
            if (!MountedWeapon) return;

            MountedWeapon.Slot = null;
            MountedWeapon.Owner = null;
            MountedWeapon = null;
        }
    }
}
