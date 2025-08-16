using System.Net;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using StorySpoiler.Models;

namespace StorySpoiler
{
    [TestFixture]
    public class StoryTests
    {
        private RestClient client;
        private static string createdStoryId;
        private const string baseUrl = "https://d3s5nxhwblsjbi.cloudfront.net";

        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetJwtToken("Ilova123", "Ilova123");

            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };

            client = new RestClient(options);
        }
        private string GetJwtToken(string username, string password)
        {
            var loginClient = new RestClient(baseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);

            request.AddJsonBody(new { username, password });

            var response = loginClient.Execute(request);

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            return json.GetProperty("accessToken").GetString() ?? string.Empty;
        }

        [Test, Order(1)]
        public void CreateStorySpoiler_WithRequiredFields_ShouldReturnCreated()
        {
            var request = new RestRequest("/api/Story/Create", Method.Post);

            var story = new StoryDTO
            {
                Title = "TestStory",
                Description = "TestDescription",
                Url = ""
            };
            request.AddJsonBody(story);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), "Expected status code to be Created (201).");       

            var jsonResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(jsonResponse, Has.Property("StoryId"), "Expected response to contain 'StoryId' property.");
            Assert.That(jsonResponse.StoryId, Is.Not.Null.And.Not.Empty, "Expected StoryId to be present in the response.");
            Assert.That(jsonResponse.Msg, Is.EqualTo("Successfully created!"), "Expected success message in response.");

            createdStoryId = jsonResponse.StoryId;
        }

        [Test, Order(2)]
        public void EditCreatedStorySpoiler_ShouldReturnOk()
        {
            var request = new RestRequest($"/api/Story/Edit/{createdStoryId}", Method.Put);

            var edited = new StoryDTO
            {
                Title = "Edited Title",
                Description = "Edited Description",
                Url = ""
            };
            request.AddJsonBody(edited);
            var response = client.Execute(request);

            var jsonResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code to be OK (200).");
            Assert.That(jsonResponse.Msg, Is.EqualTo("Successfully edited"), "Expected response message to indicate successful edit.");
        }

        [Test, Order(3)]
        public void GetAllStorySpoiler_ShouldReturnListOfStorySpoilers()
        {
            var request = new RestRequest("/api/Story/All", Method.Get);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code to be OK (200).");

            var stories = JsonSerializer.Deserialize<List<StoryDTO>>(response.Content);

            Assert.That(stories, Is.Not.Empty, "Expected the list of stories to be not empty.");
            Assert.That(stories.Count, Is.GreaterThan(0), "Expected the list of stories to contain at least one item.");
            Assert.That(stories.Any(), Is.True, "Expected the list of stories to contain items.");
        }

        [Test, Order(4)]
        public void DeleteCreatedStorySpoiler_ShouldReturnOk()
        {
            var request = new RestRequest($"/api/Story/Delete/{createdStoryId}", Method.Delete);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code to be OK (200).");
            Assert.That(response.Content, Does.Contain("Deleted successfully!"), "Expected response message to indicate successful deletion.");

            var jsonResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(jsonResponse.Msg, Is.EqualTo("Deleted successfully!"), "Expected Msg to indicate successful deletion.");
        }

        [Test, Order(5)]
        public void CreateStorySpoiler_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var request = new RestRequest("/api/Story/Create", Method.Post);

            var story = new StoryDTO
            {
                Title = "",
                Description = "",
                Url = ""
            };
            request.AddJsonBody(story);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code to be BadRequest (400).");
        }

        [Test, Order(6)]
        public void EditNonExistingStorySpoiler_ShouldReturnNotFound()
        {
            var nonExistingStoryId = 559999999;
            var request = new RestRequest($"/api/Story/Edit/{nonExistingStoryId}", Method.Put);

            var edited = new StoryDTO
            {
                Title = "Edited Title",
                Description = "Edited Description",
                Url = ""
            };

            request.AddJsonBody(edited);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "Expected status code to be NotFound (404).");
            Assert.That(response.Content, Does.Contain("No spoilers..."), "Expected response message to indicate that the Story Spoiler was not found.");
           
            var jsonResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(jsonResponse.Msg, Is.EqualTo("No spoilers..."), "Expected response message to indicate that the Story Spoiler was not found.");
        }

        [Test, Order(7)]
        public void DeleteNonExistingStorySpoiler_ShouldReturnBadRequest()
        {
            var nonExistingStoryId = 559999999;

            var request = new RestRequest($"/api/Story/Delete/{nonExistingStoryId}", Method.Delete);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code to be BadRequest (400).");
            Assert.That(response.Content, Does.Contain("Unable to delete this story spoiler!"), "Expected response message to indicate that the story spoiler could not be deleted.");

            var jsonResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(jsonResponse.Msg, Is.EqualTo("Unable to delete this story spoiler!"), "Expected response message to indicate that the Story Spoiler could not be deleted.");
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            client?.Dispose();
        }
    }
}