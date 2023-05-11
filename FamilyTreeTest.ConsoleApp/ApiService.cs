
namespace FamilyTreeTest.ConsoleApp
{
	using System;
	using System.Net.Http;
	using System.Threading.Tasks;
	using FamilyTreeTest.API.Models;
	using Newtonsoft.Json;

	public class ApiService
	{
		private readonly HttpClient _httpClient;

		public ApiService(HttpClient httpClient)
		{
			_httpClient = httpClient;
		}

		public async Task<IEnumerable<Person>> GetPeopleAsync()
		{
			var response = await _httpClient.GetAsync("https://localhost:7091/Person");
			var content = await response.Content.ReadAsStringAsync();

			if (!response.IsSuccessStatusCode)
			{
				throw new Exception($"Error retrieving data: {content}");
			}

			return JsonConvert.DeserializeObject<IEnumerable<Person>>(content);
		}

		public async Task PostPersonAsync(Person person)
		{
			var json = JsonConvert.SerializeObject(person);
			var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

			var response = await _httpClient.PostAsync("https://localhost:7091/Person", content);

			if (!response.IsSuccessStatusCode)
			{
				var responseContent = await response.Content.ReadAsStringAsync();
				throw new Exception($"Error posting data: {responseContent}");
			}
		}
	}

}
