using System;
using Pathfinding;
using Ships;
using UnityEngine;

public class AiTargetFollower : MonoBehaviour
{
    public Transform Target;
    public AIPath Agent;
    public ShipBase Ship;
    private void Start()
    {
       
    }

    private void Update()
    {
        if (Target)
        {
            Agent.maxSpeed = Ship.ShipStats.GetStat(StatType.MoveSpeed).Current;
            Agent.rotationSpeed = Ship.ShipStats.GetStat(StatType.TurnSpeed).Current;
            Agent.maxAcceleration = Ship.ShipStats.GetStat(StatType.Acceleration).Current;
            Agent.destination = Target.position;
        }
    }
}
