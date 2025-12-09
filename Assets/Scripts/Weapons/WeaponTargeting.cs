using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;

namespace Tanks
{
    public class WeaponTargeting : MonoBehaviour
    {
        public enum AimMode { Auto, Mouse }
        public AimMode Mode = AimMode.Mouse;

        public WeaponBase Weapon;
        public WeaponSlot Slot;

        private UniversalTargetingSystem targeting = new();

        private void Start()
        {
            if (!Slot)
                Slot = GetComponentInParent<WeaponSlot>();

            targeting.Init(Slot, Weapon);
        }

        public void UpdateTargetList(IEnumerable<ITargetable> targets)
        {
            targeting.UpdateTargets(targets);
        }

        private void Update()
        {
            if (Slot == null || Weapon == null)
                return;

            if (Mode == AimMode.Mouse)
            {
                AimByMouse();
                return;
            }

            // AUTO
            var target = targeting.GetTarget();
            if (target == null)
                return;

            Vector3 aimPoint = targeting.GetAimPoint(target);
            Weapon.TickWeaponPosition(aimPoint);
            if (targeting.IsAimedAt(target))
                Weapon.TickWeapon(target.Transform);
        }

        private void AimByMouse()
        {
            Vector3? mouse = GetMousePoint();
            if (mouse == null)
                return;

            Weapon.TickWeaponPosition(mouse.Value);
        }

        private Vector3? GetMousePoint()
        {
            Vector2 m = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(m);

            Plane g = new Plane(Vector3.up, Vector3.zero);
            if (g.Raycast(ray, out float d))
                return ray.GetPoint(d);

            return null;
        }
    }

}