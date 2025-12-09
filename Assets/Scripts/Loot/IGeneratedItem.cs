namespace Tanks
{
	public interface IGeneratedItem
	{
		string ItemId { get; }
		string TemplateId { get; }
		string Name { get; }
		string Rarity { get; }
	}
}