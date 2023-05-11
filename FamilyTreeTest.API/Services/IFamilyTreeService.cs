namespace FamilyTreeTest.API.Services
{
	public interface IFamilyTreeService
	{
		Task CreateFamilyTree(int motherId, string motherName, int fatherId, string fatherName);
		Task Marry(int id, int otherId, string otherName, string otherGender);
		Task HaveAKid(int motherId, int fatherId, int kidId, string kidName, string kidGender);
		Task Divorce(int motherId, int fatherId, string custody);
		string Show();
	}
}
