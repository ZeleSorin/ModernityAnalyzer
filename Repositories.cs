using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ModernityAnalyzer;

public class Repositories
{

    private const string FileName = "beautified_repos.json";

    private readonly string _filePath;

    public List<Repo> Items { get; private set; }


    public Repositories(string baseDirectory = null)
    {
        try
        {
            _filePath = Path.Combine(baseDirectory ?? Directory.GetCurrentDirectory(), FileName);
            Items = new List<Repo>();
            Load();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing repositories: {ex.Message}");
        }
    }

    private void Load()
    {
        if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
        {
            Console.WriteLine($"Filepath could not be resolved: {_filePath}");
            return;
        }

        try
        {
            // Read the raw JSON content
            var rawJson = File.ReadAllText(_filePath);
            //Console.WriteLine($"Raw JSON content: {rawJson}");

            // Deserialize the JSON into a list of Repo objects
            var jsonRepos = JsonSerializer.Deserialize<List<Repo>>(rawJson);

            // Add the deserialized objects to the Items list
            Items.AddRange(jsonRepos);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading repositories: {ex.Message}");
        }
    }
}



public class Repo
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("clone_url")]
    public string CloneUrl { get; set; }

    [JsonPropertyName("html_url")]
    public string Url { get; set; }

    [JsonPropertyName("stargazers_count")]
    public int StarCount { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("commits_url")]
    public string CommitsUrl { get; set; }

    public Repo() { }

    public string Summary()
    {
        var strBuilder = new StringBuilder();
        strBuilder.AppendLine($"Name: {Name}");
        strBuilder.AppendLine($"Url: {Url}");
        strBuilder.AppendLine($"CloneUrl: {CloneUrl}");
        strBuilder.AppendLine($"StarCount: {StarCount}");
        strBuilder.AppendLine($"Size: {Size}");
        strBuilder.AppendLine($"CommitsUrl: {CommitsUrl}");

        return strBuilder.ToString();
    }

    override
    public string ToString()
    {
        return Summary();
    }
}