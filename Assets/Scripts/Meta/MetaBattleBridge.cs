namespace Tanks
{
	public static class MetaBattleBridge
	{
		public static MetaState LastFit;

		public static void LoadFit(MetaState state)
		{
			LastFit = state;
		}
	}

}