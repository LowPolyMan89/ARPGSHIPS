using UnityEngine;

namespace Tanks
{
	public interface ITargetable : IStatsProvider
	{
		Transform Transform { get; }
		bool IsAlive { get; }
		TargetSize Size { get; }
		Vector2 Velocity { get; }
		TeamMask Team { get; }
		void TakeDamage(CalculatedDamage calculatedDamage);
	}

}