using LibGit2Sharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernityAnalyzer;

public class Worker
{
    public async Task<List<CommitResults>> WorkThisRepo(Repo repo, string repoDir)
    {
        var commitsDeck = new List<CommitResults>();
       
            ClearDirectory(repoDir);

            using var workspace = MSBuildWorkspace.Create();


            Console.WriteLine($"Working with repository: {repo.Name}");

            var repoPath = Path.Combine(repoDir, repo.Name);

            Repository.Clone(repo.CloneUrl, repoPath);
            

            using var currentRepo = new Repository(repoPath);

            var commits = currentRepo.Commits.ToList();
            Console.WriteLine($"Number of commits: {commits.Count}");
            if (commits.Count < 300)
            {
                Console.WriteLine($"Repository {repo.Name} has less than 300 commits. Skipping analysis.");
                return null;
            }
            if (commits.First().Committer.When.DateTime < DateTime.Now.AddYears(-1))
            {
                Console.WriteLine($"Repository {repo.Name} has commits older than 1 years. Skipping analysis.");
                return null;
            }
            var commitCount = 0;
            var analyzer = new SolutionAnalyzer();
            var counter = 1;
            var splitter = commits.Count / 100;
            commits.Reverse();
            var paralelCounter = 0;
            if(commits.Last().Committer.When.DateTime < DateTime.UtcNow.AddYears(-1))
            {
                Console.WriteLine($"\n^^^___^^^DEBUG SORIN^^^___^^^: Commits too old {commits.Last().Committer.When.DateTime} \n");
                return null;
            }
            Console.WriteLine($"Number of commits for repository: {repo.Name} is {commits.Count} and we are creating");
            Console.WriteLine($"batches of {splitter} commits!");
            foreach (var commit in commits)
            {
                try
                {
               // Console.WriteLine($"Commit date: {commit.Committer.When.DateTime.ToString("MM-dd-yyyy")} with splitter: {splitter} and counter: {commitCount}");
                
                if (commit.Committer.When.DateTime > DateTime.UtcNow.AddYears(-6) && commitCount == splitter)
                {
                    
                    commitCount = 0;
                    Console.WriteLine($"Batch number {counter} at date {commit.Committer.When.DateTime.ToString("MM-dd-yy")}");

                    Commands.Checkout(currentRepo, commit);
                    
                    var solutionPath = Directory.GetFiles(repoPath, "*.sln", SearchOption.AllDirectories).FirstOrDefault();
                    var allProjects = Directory.GetFiles(repoPath, "*.csproj", SearchOption.AllDirectories).ToList();
                    //Console.WriteLine($"Number of projects: {allProjects.Count}");
                    if (solutionPath is null || solutionPath.Equals(""))
                    {
                        Console.WriteLine($"No solution file found in {repoPath}. Skipping analysis.");
                        continue;
                    }
                    /* workspace.WorkspaceFailed += (sender, e) =>
                     {
                         Console.WriteLine($"Workspace load error: {e.Diagnostic.Message}");
                     };*/
                    //Console.WriteLine($"{solutionPath}");
                    var solution = await workspace.OpenSolutionAsync(solutionPath);
                    // var projects = solution.Projects.ToList();
                    var commitResults = analyzer.AnalyzeRepo(solution, repoPath, commit.Committer.When.DateTime.ToString("dd-MM-yyyy"));

                    commitsDeck.Add(commitResults);
                    paralelCounter++;
                    counter++;
                }
                else
                {
                    commitCount++;
                }
                if(commitCount == splitter +1)
                {
                    commitCount = 0;
                }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n^^^___^^^DEBUG SORIN^^^___^^^: An error occurred while processing repository {repo.Name}: {ex.Message}");
                    Console.WriteLine($"\n^^^___^^^DEBUG SORIN^^^___^^^: {ex.StackTrace}");
            
                }
            }
            if (paralelCounter == 0)
            {
                Console.WriteLine($"\n^^^___^^^DEBUG SORIN^^^___^^^: No commit was analysed for this repo!!! {commitsDeck.Count}");
                return null;
            }
            //here save the results for the current repo
            Console.WriteLine($"One repo is done.");
            currentRepo.Dispose();

            
            return commitsDeck;
       
    }

    static void ClearDirectory(string path)
    {
        //Delete all files in the directory
        if (Directory.Exists(path))
        {
            foreach (var file in Directory.GetFiles(path))
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }
            //Continue by deleting all subdirectories
            foreach (var dir in Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
            {
                foreach (var file in Directory.GetFiles(dir))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }

            }
            Directory.GetDirectories(path).ToList().ForEach(dir => Directory.Delete(dir, true));

        }
        else { Console.WriteLine($"Directory {path} does not exist."); }
        Console.WriteLine("Directory was emptied!");
    }

}
