using FamilyTreeTest.API.Models;
using Microsoft.AspNetCore.Mvc;
using Neo4jClient;
using System.Text;

namespace FamilyTreeTest.API.Services
{
	public class FamilyTreeService : IFamilyTreeService
	{
		private readonly IGraphClient _client;

        public FamilyTree FamilyTree { get; set; }

        public FamilyTreeService(IGraphClient client)
		{
			_client = client;
			FamilyTree = new FamilyTree();
		}

		public async Task CreateFamilyTree(int motherId, string motherName, int fatherId, string fatherName)
		{
			// Create the mother and the father
			var mother = new Person { Id = motherId, Name = motherName, Gender = "Female" };
			var father = new Person { Id = fatherId, Name = fatherName, Gender = "Male" };

			await _client.Cypher.Create("(p:Person $person)")
					.WithParam("person", mother)
					.ExecuteWithoutResultsAsync();

			await _client.Cypher.Create("(p:Person $person)")
								.WithParam("person", father)
								.ExecuteWithoutResultsAsync();

			// Create the marriedTo relationship between the two persons
			await _client.Cypher.Match("(p1:Person), (p2:Person)")
								.Where((Person p1, Person p2) => p1.Id == motherId && p2.Id == fatherId)
								.Create("(p1)-[r:marriedTo]->(p2)")
								.ExecuteWithoutResultsAsync();

			// Add mother and father as root rarents nodes to the family tree
			this.FamilyTree.Root = new FamilyRelationship { Person1 = mother, Person2 = father, FamilyRelationshipType = "marriedTo" };
		}


		public async Task Divorce(int motherId, int fatherId, string custody)
		{
			// Delete the marriedTo relationship between the mother and father
			await _client.Cypher
				.Match("(mother:Person)-[r:marriedTo]-(father:Person)")
				.Where((Person mother, Person father) => mother.Id == motherId && father.Id == fatherId)
				.Delete("r")
				.ExecuteWithoutResultsAsync();

			// Create new custody relationships between the parent and their children
			if (custody == "With Mother")
			{
				await _client.Cypher
					.Match("(mother:Person)-[:parentOf]-(child:Person)")
					.Where((Person mother) => mother.Id == motherId)
					.Merge("(mother)-[:hasCustody]->(child)")
					.Set("child.custody = 'With Mother'")
					.ExecuteWithoutResultsAsync();
			}
			else if (custody == "With Father")
			{
				await _client.Cypher
					.Match("(father:Person)-[:parentOf]-(child:Person)")
					.Where((Person father) => father.Id == fatherId)
					.Merge("(father)-[:hasCustody]->(child)")
					.Set("child.custody = 'With Father'")
					.ExecuteWithoutResultsAsync();
			}
		}

		public async Task HaveAKid(int motherId, int fatherId, int kidId, string kidName, string kidGender)
		{
			var kid = new Person
			{
				Id = kidId,
				Name = kidName,
				Gender = kidGender
			};

			await _client.Cypher.Create("(p:Person $person)")
					.WithParam("person", kid)
					.ExecuteWithoutResultsAsync();


			await _client.Cypher.Match("(p1:Person), (p2:Person)")
								.Where((Person p1, Person p2) => p1.Id == motherId && p2.Id == kidId)
								.Create("(p1)-[r:parentOf]->(p2)")
								.ExecuteWithoutResultsAsync();

			await _client.Cypher.Match("(p1:Person), (p2:Person)")
					.Where((Person p1, Person p2) => p1.Id == fatherId && p2.Id == kidId)
					.Create("(p1)-[r:parentOf]->(p2)")
					.ExecuteWithoutResultsAsync();
		}

		public async Task Marry(int id, int otherId, string otherName, string otherGender)
		{
			// Check if the person with the given id is already married or has kids from a previous marriage
			var person = await _client.Cypher.Match("(p:Person)-[:marriedTo]-(), (p)-[:parentOf]->()")
										   .Where((Person p) => p.Id == id)
										   .Return(p => p.As<Person>()).ResultsAsync;

			if (person.Count() > 0)
			{
				return;
			}

			// Create the other person
			var otherPerson = new Person
			{
				Id = otherId,
				Name = otherName,
				Gender = otherGender
			};

			await _client.Cypher.Create("(p:Person $person)")
					.WithParam("person", otherPerson)
					.ExecuteWithoutResultsAsync();

			// Create the marriedTo relationship between the two persons
			await _client.Cypher.Match("(p1:Person), (p2:Person)")
								.Where((Person p1, Person p2) => p1.Id == id && p2.Id == otherId)
								.Create("(p1)-[r:marriedTo]->(p2)")
								.ExecuteWithoutResultsAsync();
		}



		public string Show()
		{
			var root = FamilyTree.Root;

			if (root == null)
			{
				return "No family tree found.";
			}

			var sb = new StringBuilder();
			sb.AppendLine($"ROOT: {root.Person1.Name} ({root.Person1.Gender}) married to {root.Person2.Name} ({root.Person2.Gender})");

			// Traverse the family tree recursively
			TraverseTree(root.Person1, sb, 1);

			return sb.ToString();
		}

		private void TraverseTree(Person person, StringBuilder sb, int depth)
		{
			// Find the spouses and kids of the current person
			var spouses = _client.Cypher
				.Match("(p:Person)-[:marriedTo]->(spouse:Person)")
				.Where((Person p) => p.Id == person.Id)
				.Return((p, spouse) => spouse.As<Person>())
				.ResultsAsync;

			var kids = _client.Cypher
				.Match("(p:Person)-[:parentOf]->(kid:Person)")
				.Where((Person p) => p.Id == person.Id)
				.Return(kid => kid.As<Person>())
				.ResultsAsync;

			// Print the current person and their properties
			foreach (var spouse in spouses.Result)
			{
				sb.AppendLine($"{new string('\t', depth)}- {person.Name} ({person.Gender}) married to {spouse.Name} ({spouse.Gender})");
				TraverseTree(spouse, sb, depth + 1);
			}

			foreach (var kid in kids.Result)
			{
				sb.AppendLine($"{new string('\t', depth)}- {kid.Name} ({kid.Gender})");
				TraverseTree(kid, sb, depth + 1);
			}
		}


	}
}
