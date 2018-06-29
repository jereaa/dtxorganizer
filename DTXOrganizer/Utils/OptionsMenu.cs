using System;
using System.Collections.Generic;
using System.Text;

public class OptionsMenu {
    
    private struct MenuOption {
        public string Option;
        public Action OnSelected;
    }

    private readonly List<MenuOption> _menuOptions = new List<MenuOption>();
    private int _currentHighlightedOption = 0;
    private int _consoleBufferTopStart;

    public void Add(string option, Action onSelected) {
        _menuOptions.Add(new MenuOption {
            Option = option,
            OnSelected = onSelected
        });
    }

    public void DisplayMenu(string prompt) {
        if (_menuOptions.Count == 0) {
            return;
        }
        
        Console.WriteLine(prompt);
        _consoleBufferTopStart = Console.CursorTop;
        
        for (int i = 0 ; i < _menuOptions.Count ; i++) {
            Console.WriteLine($"    {i+1:D2} - {_menuOptions[i].Option}");
        }
        
        HighlightOption(0);

        ConsoleKeyInfo keyPressed = Console.ReadKey(true);
        while (keyPressed.Key != ConsoleKey.Enter) {
            if (keyPressed.Key == ConsoleKey.UpArrow) {
                HighlightOption(_currentHighlightedOption - 1);
            } else if (keyPressed.Key == ConsoleKey.DownArrow) {
                HighlightOption(_currentHighlightedOption + 1);
            }

            keyPressed = Console.ReadKey(true);
        }

        Console.CursorTop = _consoleBufferTopStart - 1;
        Console.CursorLeft = 0;
        StringBuilder sb = new StringBuilder((_menuOptions.Count + 1) * Console.BufferWidth);
        sb.Append(' ', (_menuOptions.Count + 1) * Console.BufferWidth);
        Console.WriteLine(sb);
        sb.Clear();
        Console.CursorTop = _consoleBufferTopStart - 1;
        
        _menuOptions[_currentHighlightedOption].OnSelected();
    }

    private void HighlightOption(int option) {
        if (option < 0 || option >= _menuOptions.Count) {
            return;
        }

        Console.CursorTop = _consoleBufferTopStart + _currentHighlightedOption;
        Console.CursorLeft = 0;
        Console.Write("   ");
        
        Console.CursorTop = _consoleBufferTopStart + option;
        Console.CursorLeft = 0;
        Console.Write("-->");

        _currentHighlightedOption = option;
    }

}
