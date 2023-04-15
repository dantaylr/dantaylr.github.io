using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

public class Program
{
    public static async Task Main()
    {
        int choice = 0;
        await DialogueSystem.Say("What do you want for dinner?");

        string[] choices = new string[] { "Pizza", "Nothing" };
        choice = await DialogueSystem.Choice("Choose one:", choices);

        if (choice == 1)
        {
            await DialogueSystem.Say("Are you sure?");
            string[] yesNoChoices = new string[] { "Yes", "No" };
            choice = await DialogueSystem.Choice("Choose one:", yesNoChoices);
        }
    }
}

public class DialogueSystem
{
    public static async Task Say(string message)
    {
        Console.WriteLine(message);
        await Task.CompletedTask;
    }
    
    public static async Task<int> Choice(string message, string[] choices)
    {
        Console.WriteLine(message);

        for (int i = 0; i < choices.Length; i++)
        {
            Console.WriteLine((i + 1)+":"+choices[i]);
        }

        int choice;
        while (true)
        {
            Console.Write("Enter your choice: ");
            string input = Console.ReadLine();

            if (int.TryParse(input, out choice) && choice >= 1 && choice <= choices.Length)
            {
                break;
            }

            Console.WriteLine("Invalid choice. Please enter a number between 1 and {0}.", choices.Length);
        }

        return choice;
    }
}

public class DialogueParser
{
    private static string[] globalBlackboard = new string[0];
    private string[] localBlackboard = new string[0];

    public async Task ParseDialogue(string fileName)
    {
        using var httpClient = new HttpClient();
        var fileUrl = $"http://imdantaylor.com/{fileName}";
        var dialogueText = await httpClient.GetStringAsync(fileUrl);

        var dialogueLines = dialogueText.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        var currentIndex = 0;

        while (currentIndex < dialogueLines.Length)
        {
            var currentLine = dialogueLines[currentIndex].Trim();

            if (!string.IsNullOrWhiteSpace(currentLine) && currentLine[0] != '-')
            {
                var choiceOptions = new List<string>();
                var currentDialogue = currentLine;

                while (currentIndex + 1 < dialogueLines.Length && dialogueLines[currentIndex + 1].TrimStart()[0] == '-')
                {
                    currentIndex++;
                    var commandLine = dialogueLines[currentIndex].Trim();
                    var commandContent = commandLine.Substring(1).Trim();

                    switch (commandLine[1])
                    {
                        case '?':
                            choiceOptions.Add(commandContent);
                            break;
                        case 'x':
                            return;
                        case 'b':
                            localBlackboard = localBlackboard.Append(commandContent).ToArray();
                            break;
                        case 'B':
                            globalBlackboard = globalBlackboard.Append(commandContent).ToArray();
                            break;
                        case 'i':
                            var conditionals = commandContent.Split('&');

                            foreach (var conditional in conditionals)
                            {
                                var parts = conditional.Split('=');

                                if (parts.Length == 2)
                                {
                                    var key = parts[0].Trim();
                                    var value = parts[1].Trim();

                                    var contains = globalBlackboard.Contains(value);
                                    var isTrue = key.StartsWith("!")
                                        ? !contains
                                        : contains;

                                    if (!isTrue)
                                    {
                                        goto EndConditional;
                                    }
                                }
                            }
                        EndConditional:
                            break;
                    }

                    if (currentIndex + 1 == dialogueLines.Length)
                    {
                        break;
                    }
                }

                var choiceArray = choiceOptions.Count > 0 ? choiceOptions.ToArray() : null;
                var result = await DialogueSystem.Say(currentDialogue, choiceArray);

                if (choiceArray != null)
                {
                    var selectedOption = choiceOptions[result - 1];
                    var indentedDialogue = "";

                    currentIndex++;

                    while (currentIndex < dialogueLines.Length && dialogueLines[currentIndex].TrimStart()[0] == '\t')
                    {
                        indentedDialogue += dialogueLines[currentIndex].TrimStart() + Environment.NewLine;
                        currentIndex++;
                    }

                    var nestedParser = new DialogueParser { localBlackboard = localBlackboard };
                    await nestedParser.ParseDialogueString(indentedDialogue);

                    localBlackboard = nestedParser.localBlackboard;
                }
            }

            currentIndex++;
        }
    }
}
