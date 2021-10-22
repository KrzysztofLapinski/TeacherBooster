using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text.Json;
using System.Threading.Tasks;
using GitHubApi;
using GitHubApi.Dto;
using ReactEduEvaluator.Dto;

namespace ReactEduEvaluator
{
    class Program
    {
        private static SettingsDto _settings;
        static async Task Main(string[] args)
        {
            await ReadSettings();

            await MainMenu();
        }

        private static async Task MainMenu()
        {
            Console.WriteLine("1. Clone/Pull from repositories");
            Console.WriteLine("2. Run npm install for selected lab");
            Console.WriteLine("3. Exit");
            var selection = Console.ReadLine();
            switch (selection)
            {
                case "1":
                    await CloneOrPullFromRepositories();
                    await MainMenu();
                    break;
                case "2":
                    await LabMenu();
                    break;
                case "3":
                    break;
                default:
                    await MainMenu();
                    break;
            }
        }

        private static async Task LabMenu()
        {
            Console.WriteLine("1. lab1");
            Console.WriteLine("2. lab2");
            Console.WriteLine("3. lab3");
            Console.WriteLine("4. lab4");
            Console.WriteLine("5. lab5");
            Console.WriteLine("6. lab6");
            Console.WriteLine("7. lab7");
            Console.WriteLine("8. lab8");
            Console.WriteLine("9. lab9");
            Console.WriteLine("10. lab10");
            Console.WriteLine("0. to return to main menu");
            var selection = Console.ReadLine();
            if (selection == "0")
            {
                await MainMenu();
            }
            else
            {
                await InstallLabsAsync(selection);
                await MainMenu();
            }
        }

        private static async Task InstallLabsAsync(string labNumber)
        {
            var studentWorkDirectories = Directory.GetDirectories(_settings.StudentsRepositoriesPath);//TODO
            Console.WriteLine($"Found {studentWorkDirectories.Length} in {_settings.StudentsRepositoriesPath} for lab{labNumber}");

            var labDirectories = studentWorkDirectories.Select(x => $"{x}\\lab{labNumber}").ToList();
            var installationTasks = DivideToChunkTasks(labDirectories);

            await Task.WhenAll(installationTasks);
        }

        private static IList<Task> DivideToChunkTasks(IList<string> labDirectories)
        {
            var numberOfChunks = 6;
            var chunkSize = labDirectories.Count / numberOfChunks;
            var tasks = new List<Task>();
            for (int i = 0; i < labDirectories.Count; i += chunkSize)
            {
                var labDirectoriesChunk = labDirectories.Skip(i).Take(chunkSize).ToList();
                tasks.Add(InstallChunkLabAsync(labDirectoriesChunk));
            }

            return tasks;
        }

        private static async Task InstallChunkLabAsync(IList<string> labDirectories)
        {
            foreach (var labDirectory in labDirectories)
            {
                using PowerShell powershell = PowerShell.Create();
                if (!Directory.Exists(labDirectory))
                {
                    Console.WriteLine($"!lab2 doesnt exist in {labDirectory}!");
                }
                else
                {
                    powershell.AddScript($"cd {labDirectory}");
                    powershell.AddScript($"npm install");
                    await powershell.InvokeAsync();

                    Console.WriteLine($"npm installed in {labDirectory} at {DateTime.Now}");
                }
            }
        }

        private static async Task CloneOrPullFromRepositories()
        {
            var githubApiClient = new GithubApiClient(_settings.GithubToken);

            Console.WriteLine("Retrieving repositories started");
            var repositories = await githubApiClient.GetRepositoriesFromOrganisationAsync(_settings.OrganizationName);
            Console.WriteLine($"Retrieving repositories finished. {repositories.Count} found");

            Console.WriteLine("Pulling/Cloning repositories started");
            CloneOrPullRepositories(repositories, _settings.StudentsRepositoriesPath);
            Console.WriteLine("Pulling/Cloning repositories finished");
        }

        private static async Task ReadSettings()
        {
            const string settingsFileName = "settings.json";
            if (File.Exists(settingsFileName))
            {
                Console.WriteLine($"Setting file ({settingsFileName}) loaded!");
                var settingsFileContent = File.ReadAllText($"{settingsFileName}");
                _settings = JsonSerializer.Deserialize<SettingsDto>(settingsFileContent);

                Console.WriteLine($"You are connected to github organization: {_settings.OrganizationName}");
            }
            else
            {
                Console.WriteLine("It seems it is your first usage of our app. We need some information from you :)");
                Console.WriteLine($"All information will be saved in {settingsFileName}. You can change it any time");

                Console.WriteLine("Enter organization name: ");
                var organizationName = Console.ReadLine();

                Console.WriteLine(
                    "Enter your personal github token (more info how to generate token can be found here: https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/creating-a-personal-access-token): ");
                var githubToken = Console.ReadLine();

                Console.WriteLine("Enter path were you want to save students repositories: ");
                var studentsRepositoriesPath = Console.ReadLine();

                _settings = new SettingsDto
                {
                    OrganizationName = organizationName,
                    GithubToken = githubToken,
                    StudentsRepositoriesPath = studentsRepositoriesPath
                };

                using (FileStream fileStream = File.Create(settingsFileName))
                {
                    await JsonSerializer.SerializeAsync(fileStream, _settings);
                }
            }
        }

        private static void CloneOrPullRepositories(IList<GithubRepositoryDto> repositories, string studentsRepositoriesPath)
        {
            foreach (var repository in repositories)
            {
                using PowerShell powershell = PowerShell.Create();
                powershell.AddScript($"cd {studentsRepositoriesPath}");

                if (Directory.Exists($"{studentsRepositoriesPath}\\{repository.Name}"))
                {
                    Console.WriteLine($"Pulling {repository.Name}");

                    powershell.AddScript($"cd {repository.Name}");
                    powershell.AddScript($"git pull");
                    Collection<PSObject> results = powershell.Invoke();
                }
                else if (repository.Name.StartsWith("Wednesday", StringComparison.InvariantCultureIgnoreCase) ||
                         repository.Name.StartsWith("Tuesday", StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine($"Cloning {repository.Name}");

                    powershell.AddScript($"git clone {repository.Clone_Url}");
                    Collection<PSObject> results = powershell.Invoke();
                }
            }
        }
    }
}
