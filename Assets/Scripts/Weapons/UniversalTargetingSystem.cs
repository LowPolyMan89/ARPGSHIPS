using System.Collections.Generic;
using System.Linq;
using Tanks;
using UnityEngine;

public class UniversalTargetingSystem
{
    public TargetSize PrioritySize = TargetSize.Small;

    private readonly List<ITargetable> _targets = new();
    private ITargetable _current;

    private IAimOrigin origin;  // << новый универсальный источник наведения
    private WeaponBase weapon;  // может быть null (только для prediction)

    public void Init(IAimOrigin origin, WeaponBase weapon = null)
    {
        this.origin = origin;
        this.weapon = weapon;
    }

    public void UpdateTargets(IEnumerable<ITargetable> list)
    {
        _targets.Clear();

        foreach (var t in list)
        {
            if (!t.IsAlive)
                continue;

            if (!HitRules.CanHit(origin.HitMask, t.Team))
                continue;

            _targets.Add(t);
        }
    }

    public ITargetable GetTarget()
    {
        if (_current == null ||
            !_current.IsAlive ||
            !IsTargetInRange(_current) ||
            !IsTargetInSector(_current))
        {
            _current = FindBestTarget();
        }

        return _current;
    }

    public ITargetable FindBestTarget()
    {
        Vector3 pos = origin.Position;

        var valid = _targets
            .Where(IsTargetInRange)
            .Where(IsTargetInSector)
            .ToList();

        if (valid.Count == 0)
            return null;

        var prio = valid
            .Where(t => t.Size == PrioritySize)
            .OrderBy(t => Vector3.Distance(pos, t.Transform.position))
            .FirstOrDefault();

        return prio ?? valid.OrderBy(t => Vector3.Distance(pos, t.Transform.position)).First();
    }

    private bool IsTargetInRange(ITargetable t)
    {
        float dist = Vector3.Distance(origin.Position, t.Transform.position);
        return dist <= origin.DetectionRange;
    }
    
    public bool IsAimedAt(ITargetable target, float toleranceDeg = 3f)
    {
        Vector3 dir = GetAimDirection(target);
        Vector3 fwd = origin.Forward;

        float angle = Vector3.Angle(fwd, dir);
        return angle <= toleranceDeg;
    }
    private bool IsTargetInSector(ITargetable t)
    {
        Vector3 dir = (t.Transform.position - origin.Position).normalized;
        float angle = Vector3.Angle(origin.Forward, dir);
        return angle <= origin.AllowedAngle;
    }

    public Vector3 GetAimPoint(ITargetable t)
    {
        if (weapon == null)
            return t.Transform.position;

        Vector3 pos = origin.Position;
        Vector3 target = t.Transform.position;
        Vector3 vel = t.Velocity;

        float speed = weapon.Model.Stats.GetStat(StatType.ProjectileSpeed).Current;

        float dist = Vector3.Distance(pos, target);
        float time = dist / speed;

        return target + vel * time;
    }

    public Vector3 GetAimDirection(ITargetable t)
    {
        Vector3 aim = GetAimPoint(t);
        Vector3 dir = aim - origin.Position;
        dir.y = 0;
        return dir.normalized;
    }
}
