namespace FamilyTreeTest.API.Models
{
	public class FamilyRelationship
	{
		public string FamilyRelationshipType { get; set; }
		public Person Person1 { get; set; }
		public Person Person2 { get; set; }
	}
}
