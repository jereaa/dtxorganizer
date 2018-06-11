using System;
using System.Text;

public class UserPrompt : IDisposable {

    private int consoleTop;

    public int PromptUserForChoice(string prompt, string[] choices, string currentValue = null,
                                    bool keepAsIsChoice = true, bool removeOption = true) {
        
        StringBuilder outputBuilder = new StringBuilder();
        outputBuilder.AppendLine($"\n{prompt}");
        
        int i;
        for (i = 0; i < choices.Length; i++) {
            outputBuilder.AppendLine($"\t{i}_ {choices[i]}");
        }

        if (keepAsIsChoice) {
            outputBuilder.AppendLine($"\t{i++}_ Keep value as it is.");
        }
        
        if (removeOption) {
            outputBuilder.AppendLine($"\t{i}_ Remove property.");
        }

        if (currentValue != null) {
            outputBuilder.AppendLine($"Current value is '{currentValue}'");
        }

        outputBuilder.AppendLine();
        outputBuilder.Append("User choice: ");

        // Save cursor Y position
        consoleTop = Console.CursorTop;
        Console.Write(outputBuilder);

        int userChoice = -1;
        bool correctChoice = false;
        while (!correctChoice) {
            string userAnswer = Console.ReadLine();
            correctChoice = int.TryParse(userAnswer, out userChoice) && userChoice >= 0 &&
                            userChoice <= i;
        }

        outputBuilder.Clear();
        outputBuilder.Append(' ', (Console.CursorTop - consoleTop + 1) * Console.BufferWidth);
        Console.SetCursorPosition(0, consoleTop);
        Console.Write(outputBuilder);
        Console.SetCursorPosition(0, consoleTop);

        return userChoice;
    }
    
    public void Dispose() {
        
    }
}
