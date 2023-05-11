using FamilyTreeTest.API.Services;
using FamilyTreeTest.ConsoleApp;
using Microsoft.Extensions.DependencyInjection;
using Neo4jClient;

namespace MyConsoleApp
{

	class Program
	{
		static async Task Main(string[] args)
		{
			var services = new ServiceCollection();

			var client = new BoltGraphClient(new Uri("bolt://localhost:7687"), "neo4j", "bennytest");
			await client.ConnectAsync();
			services.AddSingleton<IGraphClient>(client);
			services.AddSingleton<IFamilyTreeService, FamilyTreeService>();

			var serviceProvider = services.BuildServiceProvider();
			var familyTreeService = serviceProvider.GetService<IFamilyTreeService>();

			// Create a family tree
			await familyTreeService.CreateFamilyTree(301, "Alice", 302, "Bob");

			// Marry two persons
			await familyTreeService.Marry(301, 303, "Charlie", "Male");
			await familyTreeService.Marry(302, 304, "Denise", "Female");

			// Have kids
			await familyTreeService.HaveAKid(301, 303, 305, "Eve", "Female");
			await familyTreeService.HaveAKid(302, 304, 306, "Frank", "Male");

			// Divorce a couple and assign custody to the mother
			await familyTreeService.Divorce(301, 303, "Mother");

			// Show the family tree
			Console.WriteLine(familyTreeService.Show());
		}
	}

}