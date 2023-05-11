using FamilyTreeTest.API.Models;
using Microsoft.AspNetCore.Mvc;
using Neo4jClient;

namespace FamilyTreeTest.API.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class PersonController : ControllerBase
	{
		private readonly IGraphClient _client;

		public PersonController(IGraphClient client)
		{
			_client = client;
		}

		[HttpGet]
		public async Task<IActionResult> GetPeople()
		{
			var people = await _client.Cypher.Match("(p: Person)")
												  .Return(p => p.As<Person>()).ResultsAsync;

			return Ok(people);
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetPersonById(int id)
		{
			var people = await _client.Cypher.Match("(p:Person)")
												  .Where((Person p) => p.Id == id)
												  .Return(p => p.As<Person>()).ResultsAsync;

			return Ok(people.LastOrDefault());
		}

		[HttpPost]
		public async Task<IActionResult> CreatePerson([FromBody] Person person)
		{
			await _client.Cypher.Create("(p:Person $person)")
								.WithParam("person", person)
								.ExecuteWithoutResultsAsync();
			return Ok();
		}

		[HttpGet("{fatherId}/{fatherName}/CreateFamilyTree/{motherId}/{motherName}/")]
		public async Task<IActionResult> CreateFamilyTree(int motherId, string motherName, int fatherId, string fatherName)
		{
			// Create the mother and the father
			var mother = new Person{Id = motherId,Name = motherName,Gender = "Female"};
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

			return Ok();
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> UpdatePerson(int id, [FromBody] Person person)
		{
			await _client.Cypher.Match("(p:Person)")
								.Where((Person p) => p.Id == id)
								.Set("p = $person")
								.WithParam("person", person)
								.ExecuteWithoutResultsAsync();
			return Ok();
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeletePerson(int id)
		{
			// Check if the person with the given id has custody of any kids
			var custodyRelationships = await _client.Cypher.Match("(p:Person)-[r:withMother|withFather]->(k:Person)")
														.Where((Person p) => p.Id == id)
														.Return((p, r, k) => new
														{
															Person = p.As<Person>(),
															Relationship = r.As<RelationshipInstance>(),
															Kid = k.As<Person>()
														}).ResultsAsync;

			if (custodyRelationships.Any())
			{
				// Delete the withMother or withFather relationship between the person and their kids
				foreach (var custodyRelationship in custodyRelationships)
				{
					var relationshipType = custodyRelationship.Relationship;
					await _client.Cypher.Match($"(p:Person)-[r:{relationshipType}]->(k:Person)")
											.Where((Person p, RelationshipInstance r, Person k) => p.Id == id && k.Id == custodyRelationship.Kid.Id)
											.Delete("r")
											.ExecuteWithoutResultsAsync();
				}
			}

			// Delete the person and their kids
			await _client.Cypher.Match("(p:Person)-[r]-()")
								 .Where((Person p) => p.Id == id)
								 .DetachDelete("p, r")
								 .ExecuteWithoutResultsAsync();

			return Ok();
		}


		[HttpDelete]
		public async Task<IActionResult> DeleteAllPeople()
		{
			await _client.Cypher.Match("(p:Person)")
								 .DetachDelete("p")
								 .ExecuteWithoutResultsAsync();

			return Ok();
		}

		[HttpGet("{otherId}/marriedto/{id}/")]
		public async Task<IActionResult> Marry(int id, int otherId, string otherName, string otherGender)
		{
			// Check if the person with the given id is already married or has kids from a previous marriage
			var person = await _client.Cypher.Match("(p:Person)-[:marriedTo]-(), (p)-[:parentOf]->()")
										   .Where((Person p) => p.Id == id)
										   .Return(p => p.As<Person>()).ResultsAsync;

			if (person.Count() > 0)
			{
				return BadRequest("The person is already married or has kids from a previous marriage.");
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
			return Ok();
		}


		[HttpGet("{kidId}/havekid/{motherId}/with/{fatherId}/")]
		public async Task<IActionResult> HaveAKid(int motherId, int fatherId, int kidId, string kidName, string kidGender)
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

			return Ok();
		}

		[HttpGet("{fatherId}/DivorceFromWithCustody/{motherId}/")]
		public async Task<IActionResult> Divorce(int motherId, int fatherId, string custody)
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

			return Ok();
		}


	}
}
