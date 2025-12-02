using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Ships
{
    public class WeaponTargeting : MonoBehaviour
    {
        public WeaponBase Weapon;
        public WeaponSlot Slot;

        public TargetSize PriorityType = TargetSize.Small;

        private readonly List<ITargetable> allTargets = new();
        private ITargetable currentTarget;

        private void Start()
        {
            if (!Slot)
                Slot = GetComponentInParent<WeaponSlot>();
        }

        // Вызывается WeaponController каждый кадр
        public void UpdateTargetList(IEnumerable<ITargetable> targets)
        {
            allTargets.Clear();

            foreach (var t in targets)
            {
                // 1) жив ли?
                if (!t.IsAlive) 
                    continue;

                // 2) принадлежит ли к той команде, по которой можно стрелять?
                if (!HitRules.CanHit(Slot.HitMask, (t as ShipBase).Team))
                    continue;

                allTargets.Add(t);
            }
        }

        private void Update()
        {
            if (Slot == null || Weapon == null)
                return;

            SelectTargetIfNeeded();

            if (currentTarget == null)
                return;

            Vector2 aim = GetAimPoint(currentTarget);

            Weapon.TickWeaponPosition(aim);
            Weapon.TickWeapon(currentTarget.Transform);
        }

        private void SelectTargetIfNeeded()
        {
            if (currentTarget == null ||
                !currentTarget.IsAlive ||
                !IsTargetInRange(currentTarget) ||
                !IsTargetInSector(currentTarget))
            {
                currentTarget = FindBestTarget();
            }
        }

        private ITargetable FindBestTarget()
        {
            Vector2 pos = Slot.transform.position;

            var inRange = allTargets
                .Where(t => IsTargetInRange(t))
                .Where(t => IsTargetInSector(t))
                .ToList();

            if (inRange.Count == 0)
                return null;

            // Сначала по приоритетному размеру
            var priority = inRange
                .Where(t => t.Size == PriorityType)
                .OrderBy(t => Vector2.Distance(pos, t.Transform.position))
                .FirstOrDefault();

            if (priority != null)
                return priority;

            // Иначе ближайшая цель
            return inRange
                .OrderBy(t => Vector2.Distance(pos, t.Transform.position))
                .FirstOrDefault();
        }

        private bool IsTargetInRange(ITargetable t)
        {
            float dist = Vector2.Distance(Slot.transform.position, t.Transform.position);
            return dist <= Weapon.Model.FireRange;
        }

        private bool IsTargetInSector(ITargetable t)
        {
            Vector2 dir = (t.Transform.position - Slot.transform.position).normalized;
            return Slot.IsTargetWithinSector(dir);
        }

        private Vector2 GetAimPoint(ITargetable t)
        {
            Vector2 pos = Slot.transform.position;
            Vector2 targetPos = t.Transform.position;
            Vector2 vel = t.Velocity;

            float speed = Weapon.Model.ProjectileSpeed;
            Vector2 dir;

            if (speed > 0.01f)
            {
                float dist = Vector2.Distance(pos, targetPos);
                float time = dist / speed;

                Vector2 predicted = targetPos + vel * time;
                dir = (predicted - pos).normalized;
            }
            else
            {
                dir = (targetPos - pos).normalized;
            }

            dir = ApplyAccuracy(dir);

            return pos + dir * 12f;
        }

        private Vector2 ApplyAccuracy(Vector2 dir)
        {
            float acc = Weapon.Model.Accuracy;

            if (Random.value <= acc)
                return dir;

            float maxAngle = 15f * (1f - acc);
            float a = Random.Range(-maxAngle, maxAngle) * Mathf.Deg2Rad;

            float cos = Mathf.Cos(a);
            float sin = Mathf.Sin(a);

            return new Vector2(
                dir.x * cos - dir.y * sin,
                dir.x * sin + dir.y * cos
            );
        }
    }
}
