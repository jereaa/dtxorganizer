using System;
using System.Text;

public class UserPrompt : IDisposable {

    private int _currentStringCount;

    public int PromptUserForChoice(string prompt, string[] choices) {
        StringBuilder outputBuilder = new StringBuilder();
        _currentStringCount = 0;

        outputBuilder.AppendLine(prompt);
        for (int i = 0; i < choices.Length; i++) {
            outputBuilder.AppendLine($"{i}_ {choices[i]}");
        }

        outputBuilder.AppendLine();
        outputBuilder.Append("User choice: ");

        _currentStringCount = outputBuilder.Length;
        Console.Write(outputBuilder);

        int userChoice = -1;
        bool correctChoice = false;
        while (!correctChoice) {
            string userAnswer = Console.ReadLine();
            correctChoice = int.TryParse(userAnswer, out userChoice) && userChoice >= 0 &&
                            userChoice <= choices.Length - 1;
            _currentStringCount += userAnswer.Length;
            Console.WriteLine("DEBUG --- userAnswer's length is " + userAnswer.Length);
        }

        return userChoice;
    }
    
    public void Dispose() {
        _currentStringCount = 0;
    }
}
