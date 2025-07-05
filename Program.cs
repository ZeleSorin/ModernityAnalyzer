using LibGit2Sharp;
using Microsoft.CodeAnalysis.MSBuild;
using ModernityAnalyzer;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            var repositories = new Repositories("C:\\RP\\ModernityAnalyzer");

            Console.WriteLine($"Number of initialized repositories: {repositories.Items.Count}");
            var repoDir = "C:\\RP\\repos";

            Console.WriteLine($"Destination directory set to: {repoDir}. All repositories will be cloned in this directory!");

            var resultsDir = "C:\\RP\\Results";
            Console.WriteLine($"Results directory set to: {resultsDir}. All results will be saved in this directory!");

            var repoResults = new Dictionary<string, List<CommitResults>>();
            foreach (var repo in repositories.Items)
            {
                if (File.Exists($"{resultsDir}-{repo.Name}-{DateTime.UtcNow.ToString("MM-dd-yyyy")}.json")) {
                    
                    Console.WriteLine($"Results for repository |{resultsDir}-{repo.Name}-{DateTime.UtcNow.ToString("MM-dd-yyyy")}.json| already exist. Skipping analysis.");
                    continue;
                }

                var worker = new Worker();
                Console.WriteLine($"Processing repository: {repo.Name}");

                
                var commitDeck = await worker.WorkThisRepo(repo, repoDir);
                
                
                if (commitDeck == null || commitDeck.Count == 0)
                {
                    continue;
                }
                var deck = new Dictionary<string, List<CommitResults>>();
                deck.Add(repo.Name, commitDeck);
                DumpResults(deck, $"{resultsDir}-{repo.Name}-{DateTime.UtcNow.ToString("MM-dd-yyyy")}.json");
                //Save results for each repository
            }

            //DumpResults(repoResults,$"{resultsDir}-{DateTime.UtcNow}.json");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"^^^___^^^DEBUG SORIN^^^___^^^: 'An error occurred: {ex.Message}'");
            Console.WriteLine($"^^^___^^^DEBUG SORIN^^^___^^^: 'An error occurred: {ex.StackTrace}'");
            throw ex;
        }
    }
    
    

    static void DumpResults(Dictionary<string,List<CommitResults>> data,string filePath)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true, // Makes JSON human-readable
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        string jsonString = JsonSerializer.Serialize(data, options);
        if(!File.Exists(filePath))
        {
            File.Create(filePath).Close();
        }
        File.WriteAllText(filePath, jsonString);
    }


   
}