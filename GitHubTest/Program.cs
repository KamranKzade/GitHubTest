using System.Text.Json;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

class Program
{
	static async Task Main(string[] args)
	{
		var configuration = new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("appsettings.json")
			.Build();

		using var loggerFactory = LoggerFactory.Create(builder =>
		{
			builder.AddConsole();
			builder.SetMinimumLevel(LogLevel.Information);
		});

		var logger = loggerFactory.CreateLogger<Program>();

		string personalAccessToken = configuration["Github:Token"]!;
		string baseUrl = configuration["Github:BaseUrl"]!;

		using (HttpClient client = new())
		{
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", personalAccessToken);
			client.DefaultRequestHeaders.UserAgent.ParseAdd("CSharp-GitHubAPI-Client");

			try
			{
				logger.LogInformation("Starting repository creation process...");
				string? repoFullName = await CreateRepository(client, baseUrl, logger);

				ArgumentNullException.ThrowIfNull(repoFullName, nameof(repoFullName));

				logger.LogInformation("Starting issue creation process...");
				int issueNumber = await CreateIssue(client, repoFullName, baseUrl, logger);

				logger.LogInformation("Starting to add comment to the issue...");
				await AddCommentToIssue(client, repoFullName, issueNumber, baseUrl, logger);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "An error occurred during the process.");
			}
		}
	}

	static async Task<string?> CreateRepository(HttpClient client, string baseUrl, ILogger logger)
	{
		string apiUrl = $"{baseUrl}user/repos";

		var newRepo = new
		{
			name = "New Repository",
			description = "My new repository created via API",
			@private = false
		};

		try
		{
			var response = await client.PostAsJsonAsync(apiUrl, newRepo);

			if (response.IsSuccessStatusCode)
			{
				var responseBody = await response.Content.ReadAsStringAsync();
				var responseJson = JsonDocument.Parse(responseBody);
				string? fullName = responseJson.RootElement.GetProperty("full_name").GetString();

				logger.LogInformation($"Repository created successfully: {fullName}");
				Environment.SetEnvironmentVariable("repo_full_name", fullName);
				return fullName;
			}
			else
			{
				string errorMessage = await response.Content.ReadAsStringAsync();
				logger.LogError($"Error creating repository: {response.StatusCode}\n{errorMessage}");
				throw new Exception($"Error creating repository: {response.StatusCode}\n{errorMessage}");
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error occurred while creating repository.");
			return null;
		}
	}

	static async Task<int> CreateIssue(HttpClient client, string repoFullName, string baseUrl, ILogger logger)
	{
		string apiUrl = $"{baseUrl}repos/{repoFullName}/issues";

		var newIssue = new
		{
			title = "New Issue from API",
			body = "This is an issue created via API.",
		};

		try
		{
			var response = await client.PostAsJsonAsync(apiUrl, newIssue);

			if (response.IsSuccessStatusCode)
			{
				var responseBody = await response.Content.ReadAsStringAsync();
				var responseJson = JsonDocument.Parse(responseBody);
				int issueNumber = responseJson.RootElement.GetProperty("number").GetInt32();

				logger.LogInformation($"Issue created successfully: #{issueNumber}");
				Environment.SetEnvironmentVariable("issue_number", issueNumber.ToString());
				return issueNumber;
			}
			else
			{
				string errorMessage = await response.Content.ReadAsStringAsync();
				logger.LogError($"Error creating issue: {response.StatusCode}\n{errorMessage}");
				throw new Exception($"Error creating issue: {response.StatusCode}\n{errorMessage}");
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error occurred while creating issue.");
			return -1;
		}
	}

	static async Task AddCommentToIssue(HttpClient client, string repoFullName, int issueNumber, string baseUrl, ILogger logger)
	{
		string apiUrl = $"{baseUrl}repos/{repoFullName}/issues/{issueNumber}/comments";

		var newComment = new
		{
			body = "This is a comment added via API."
		};

		try
		{
			var response = await client.PostAsJsonAsync(apiUrl, newComment);

			if (response.IsSuccessStatusCode)
			{
				logger.LogInformation("Comment added successfully!");
			}
			else
			{
				string errorMessage = await response.Content.ReadAsStringAsync();
				logger.LogError($"Error adding comment: {response.StatusCode}\n{errorMessage}");
				throw new Exception($"Error adding comment: {response.StatusCode}\n{errorMessage}");
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error occurred while adding comment to issue.");
		}
	}
}
