namespace Tanks
{
	public class HitRules
	{
		/// <summary>
		/// Может ли объект с hitMask нанести урон объекту targetTeam?
		/// </summary>
		public static bool CanHit(TeamMask hitMask, TeamMask targetTeam)
		{
			return (hitMask & targetTeam) != 0;
		}
	}
}