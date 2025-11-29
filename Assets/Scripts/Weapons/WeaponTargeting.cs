namespace Ships
{
	using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class WeaponTargeting : MonoBehaviour
{
    public WeaponBase Weapon;
    public WeaponSlot Slot;

    public TargetSize PriorityType = TargetSize.Small;

    private List<ITargetable> enemies = new();
    private ITargetable currentTarget;

    public void SetEnemies(List<ITargetable> list)
    {
        enemies = list;
    }

    void Update()
    {
        if (!Slot)
        {
            Slot = GetComponentInParent<WeaponSlot>();
        }
        if(!Slot)
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
        if (currentTarget == null)
        {
            currentTarget = FindTarget();
            return;
        }

        if (!currentTarget.IsAlive || !IsTargetInSector(currentTarget) || !IsTargetInRange(currentTarget))
        {
            currentTarget = FindTarget();
        }
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

    private ITargetable FindTarget()
    {
        Vector2 pos = Slot.transform.position;

        var inRange = enemies
            .Where(e =>
            {
                if (e.IsAlive) 
                    return true;
                return false;
            })
            .Where(e =>
            {
                if (Vector2.Distance(pos, e.Transform.position) <= Weapon.Model.FireRange)
                    return true;
                return false;
            })
            .Where(e => {
                Vector2 dir = ((Vector2)e.Transform.position - pos).normalized;
                return Slot.IsTargetWithinSector(dir);
            })
            .ToList();

        if (inRange.Count == 0)
            return null;

        var priority = inRange
            .Where(e => e.Size == PriorityType)
            .OrderBy(e => Vector2.Distance(pos, e.Transform.position))
            .FirstOrDefault();

        if (priority != null)
            return priority;

        return inRange
            .OrderBy(e => Vector2.Distance(pos, e.Transform.position))
            .FirstOrDefault();
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