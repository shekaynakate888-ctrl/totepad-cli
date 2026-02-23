namespace Totepad;

class Note
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

class TotePadApp
{
    readonly string notesFolder = "Notes";
    readonly List<Note> notes = new List<Note>();

    void StartupSequence()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Yellow;

        Console.WriteLine("==============================================");
        Console.WriteLine("=                                            =");
        Console.WriteLine("=                 WELCOME TO                 =");
        Console.WriteLine("=                  TOTEPAD                   =");
        Console.WriteLine("=                                            =");
        Console.WriteLine("==============================================\n\n\n\n\n\n\n");

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey(true);
    }
/// <summary>
/// This loads existing notes from the Note folder.
/// </summary>
    void LoadNotes()
    {
        if (!Directory.Exists(notesFolder))
            Directory.CreateDirectory(notesFolder);

        notes.Clear();

        foreach (string file in Directory.GetFiles(notesFolder, "*.txt"))
        {
            notes.Add(new Note
            {
                Title = Path.GetFileNameWithoutExtension(file),
                Content = File.ReadAllText(file)
            });
        }
    }
    bool ShowDecisionMenu(string prompt, string positiveOption, string negativeOption)
    {
        int selected = 0; // 0 = Positive (Green), 1 = Negative (Red)
        string[] options = { positiveOption, negativeOption };


        Console.WriteLine($"\n\n{prompt}");

        // Save current cursor position so we can redraw the menu without clearing the whole screen
        int menuTop = Console.CursorTop;
        Console.CursorVisible = false;

        while (true)
        {
            Console.SetCursorPosition(0, menuTop); 

            Console.Write(new string(' ', Console.WindowWidth)); 
            Console.SetCursorPosition(0, menuTop);

            for (int i = 0; i < options.Length; i++)
            {
                if (i == selected)
                {
                    // Logic for Colors: Green for Positive, Red for Negative
                    if (i == 0) Console.ForegroundColor = ConsoleColor.Green;
                    else Console.ForegroundColor = ConsoleColor.Red;

                    Console.Write($"[ {options[i]} ]   ");
                }
                else
                {
                    // Unselected items are White
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write($"  {options[i]}     ");
                }
            }
            Console.ForegroundColor = ConsoleColor.White; 

            ConsoleKey key = Console.ReadKey(true).Key;

            if (key == ConsoleKey.LeftArrow)
                selected = (selected - 1 + options.Length) % options.Length;
            else if (key == ConsoleKey.RightArrow)
                selected = (selected + 1) % options.Length;
            else if (key == ConsoleKey.Enter)
            {
                Console.CursorVisible = true;
                return selected == 0; // Returns True if Positive (Save/Yes), False if Negative
            }
        }
    }
/// <summary>
/// This displays the main menu and handles navigation to Notes, Calendar, or Exit.
/// </summary>
    void ShowMainMenu()
    {
        int selected = 0;
        string[] menu = { "Notes", "Calendar and Scheduling", "Exit" };

        while (true)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("==============================================");
            Console.WriteLine("=                  TOTEPAD                   =");
            Console.WriteLine("==============================================\n\n\n");

            for (int i = 0; i < menu.Length; i++)
            {
                if (i == selected)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[ {menu[i]} ]");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                }
                else
                    Console.WriteLine($"  {menu[i]}");
            }

            ConsoleKey key = Console.ReadKey(true).Key;

            if (key == ConsoleKey.UpArrow)
                selected = (selected - 1 + menu.Length) % menu.Length;
            else if (key == ConsoleKey.DownArrow)
                selected = (selected + 1) % menu.Length;
            else if (key == ConsoleKey.Enter)
            {
                if (selected == 0) ShowNotesMenu();
                else if (selected == 1) ShowCalendar();
                else break;
            }
        }
    }
/// <summary>
/// This displays the notes menu, allowing users to create, view, modify, or delete notes.
/// </summary>
    void ShowNotesMenu()
    {
        int selected = 0;
        string[] actions = { "Create", "View", "Modify", "Delete", "Back" };

        while (true)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("==============================================");
            Console.WriteLine("=                 NOTES LIST                 =");
            Console.WriteLine("==============================================\n");

            Console.ForegroundColor = ConsoleColor.White;
            if (notes.Count == 0)
                Console.WriteLine("\n(No notes found)\n\n");
            else
            {
                for (int i = 0; i < notes.Count; i++)
                    Console.WriteLine($"- {notes[i].Title}");
            }

            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine("\n==============================================");
            Console.WriteLine("=                 ACTIONS                    =");
            Console.WriteLine("==============================================");

            for (int i = 0; i < actions.Length; i++)
            {
                if (i == selected)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[ {actions[i]} ]");
                    Console.ForegroundColor = ConsoleColor.DarkBlue;
                }
                else
                    Console.WriteLine($"  {actions[i]}");
            }

            ConsoleKey key = Console.ReadKey(true).Key;

            if (key == ConsoleKey.UpArrow)
                selected = (selected - 1 + actions.Length) % actions.Length;
            else if (key == ConsoleKey.DownArrow)
                selected = (selected + 1) % actions.Length;
            else if (key == ConsoleKey.Enter)
            {
                if (selected == 0) CreateNote();
                else if (selected == 1) ViewNote();
                else if (selected == 2) ModifyNote();
                else if (selected == 3) DeleteNote();
                else break;
            }
        }
    }
/// <summary>
/// This feature allows users to create a new note with a title and content.
/// </summary>
    void CreateNote()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;

        Console.Write("Title: ");
        string title = Console.ReadLine()?.Trim() ?? "";
        if (string.IsNullOrEmpty(title))
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Title cannot be empty. Press any key to return...");
            Console.ReadKey(true);
            return;
        }
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("\n======================================================================");
        Console.WriteLine("= Arrow Keys: Move | Backspace: Delete | TAB: Finish Editing    =");
        Console.WriteLine("======================================================================\n");

        string content = "";
        int cursorPos = 0;
        int editStartLine = Console.CursorTop;
        
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("> ");
        int inputStartCol = Console.CursorLeft;

        // Typing Loop
        while (true)
        {
            ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);

            // TAB moves to the Save/Cancel menu
            if (keyInfo.Key == ConsoleKey.Tab)
            {
                break; 
            }

            if (keyInfo.Key == ConsoleKey.LeftArrow)
            {
                if (cursorPos > 0)
                {
                    cursorPos--;
                    UpdateCursorDisplay(content, cursorPos, editStartLine, true);
                }
            }
            else if (keyInfo.Key == ConsoleKey.RightArrow)
            {
                if (cursorPos < content.Length)
                {
                    cursorPos++;
                    UpdateCursorDisplay(content, cursorPos, editStartLine, true);
                }
            }
            else if (keyInfo.Key == ConsoleKey.UpArrow)
            {
                // Move cursor up one line
                int lineStart = content.LastIndexOf('\n', cursorPos - 1);
                if (lineStart >= 0)
                {
                    int prevLineStart = content.LastIndexOf('\n', lineStart - 1) + 1;
                    int posInLine = cursorPos - lineStart - 1;
                    int prevLineLength = lineStart - prevLineStart;
                    cursorPos = prevLineStart + Math.Min(posInLine, prevLineLength);
                    UpdateCursorDisplay(content, cursorPos, editStartLine, true);
                }
            }
            else if (keyInfo.Key == ConsoleKey.DownArrow)
            {
                // Move cursor down one line
                int currentLineStart = content.LastIndexOf('\n', cursorPos) + 1;
                int currentLineEnd = content.IndexOf('\n', currentLineStart);
                
                if (currentLineEnd >= 0 && currentLineEnd + 1 < content.Length)
                {
                    int nextLineStart = currentLineEnd + 1;
                    int nextLineEnd = content.IndexOf('\n', nextLineStart);
                    if (nextLineEnd == -1) nextLineEnd = content.Length;
                    
                    int posInLine = cursorPos - currentLineStart;
                    int nextLineLength = nextLineEnd - nextLineStart;
                    cursorPos = nextLineStart + Math.Min(posInLine, nextLineLength);
                    UpdateCursorDisplay(content, cursorPos, editStartLine, true);
                }
            }
            else if (keyInfo.Key == ConsoleKey.Home)
            {
                // Move to start of line
                int lineStart = content.LastIndexOf('\n', cursorPos) + 1;
                cursorPos = lineStart;
                UpdateCursorDisplay(content, cursorPos, editStartLine, true);
            }
            else if (keyInfo.Key == ConsoleKey.End)
            {
                // Move to end of line
                int lineEnd = content.IndexOf('\n', cursorPos);
                if (lineEnd == -1) lineEnd = content.Length;
                cursorPos = lineEnd;
                UpdateCursorDisplay(content, cursorPos, editStartLine, true);
            }
            else if (keyInfo.Key == ConsoleKey.Enter)
            {
                content = content.Insert(cursorPos, "\n");
                cursorPos++;
                Console.ForegroundColor = ConsoleColor.White;
                RedrawContent(content, cursorPos, editStartLine, true);
            }
            else if (keyInfo.Key == ConsoleKey.Backspace)
            {
                if (cursorPos > 0)
                {
                    content = content.Remove(cursorPos - 1, 1);
                    cursorPos--;
                    Console.ForegroundColor = ConsoleColor.White;
                    RedrawContent(content, cursorPos, editStartLine, true);
                }
            }
            else if (keyInfo.Key == ConsoleKey.Delete)
            {
                if (cursorPos < content.Length)
                {
                    content = content.Remove(cursorPos, 1);
                    Console.ForegroundColor = ConsoleColor.White;
                    RedrawContent(content, cursorPos, editStartLine, true);
                }
            }
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                content = content.Insert(cursorPos, keyInfo.KeyChar.ToString());
                cursorPos++;
                Console.ForegroundColor = ConsoleColor.White;
                RedrawContent(content, cursorPos, editStartLine, true);
            }
        }

        // Show the decision menu (Green Save / Red Cancel with the eme eme effect XD)
        bool save = ShowDecisionMenu("Select Action:\n\n", "Save", "Cancel");

        if (save)
        {
            notes.Add(new Note { Title = title, Content = content });
            File.WriteAllText(Path.Combine(notesFolder, $"{title}.txt"), content);
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("\n\nNote saved! Press any key go back...");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("\n\nNote Discarded. Press any key to go back...");
        }
        Console.ReadKey(true);
    }
/// <summary>
/// This feature allows users to view existing notes by selecting from their list.
/// </summary>
    void ViewNote()
    {
        if (notes.Count == 0) return;

        while (true)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Select note number (or type '0' to go back):\n\n");
            Console.ForegroundColor = ConsoleColor.White;

            for (int i = 0; i < notes.Count; i++)
                Console.WriteLine($"{i + 1}. {notes[i].Title}");

            Console.Write("\n> ");
            string input = Console.ReadLine()?.Trim() ?? "";

            if (input == "0") break;

            if (int.TryParse(input, out int choice) && choice > 0 && choice <= notes.Count)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"=== {notes[choice - 1].Title} ===");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"\n{notes[choice - 1].Content}");
                Console.WriteLine("\n\n(Press any key to return to list)");
                Console.ReadKey(true);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid selection. Please enter a valid number.");
                Thread.Sleep(1000); 
            }
        }
    }
/// <summary>
/// This feature allows users to modify existing notes.
/// </summary>
    void ModifyNote()
    {
        if (notes.Count == 0)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("No notes to modify. Press any key to exit...");
            Console.ReadKey(true);
            return;
        }

        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Select note to modify:\n\n");
        for (int i = 0; i < notes.Count; i++)
            Console.WriteLine($"{i + 1}. {notes[i].Title}");

        Console.Write("\n> ");
        if (int.TryParse(Console.ReadLine(), out int choice) && choice > 0 && choice <= notes.Count)
        {
            Note noteToEdit = notes[choice - 1];
            
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"Editing: {noteToEdit.Title}\n");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("====================================================================");
            Console.WriteLine("= Arrow Keys: Move | Backspace: Delete | TAB: Finish Editing     =");
            Console.WriteLine("====================================================================\n");
            
            string content = noteToEdit.Content;
            int cursorPos = content.Length;
            int editStartLine = Console.CursorTop;
            
            // Display initial content
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(content);
            UpdateCursorDisplay(content, cursorPos, editStartLine);
            
            // Editing Loop
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.White;
                ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
                Console.ForegroundColor = ConsoleColor.White;

                if (keyInfo.Key == ConsoleKey.Tab)
                {
                    break; // Exit typing, go to menu
                }

                if (keyInfo.Key == ConsoleKey.LeftArrow)
                {
                    if (cursorPos > 0)
                    {
                        cursorPos--;
                        UpdateCursorDisplay(content, cursorPos, editStartLine);
                    }
                }
                else if (keyInfo.Key == ConsoleKey.RightArrow)
                {
                    if (cursorPos < content.Length)
                    {
                        cursorPos++;
                        UpdateCursorDisplay(content, cursorPos, editStartLine);
                    }
                }
                else if (keyInfo.Key == ConsoleKey.UpArrow)
                {
                    // Move cursor up one line
                    int lineStart = content.LastIndexOf('\n', cursorPos - 1);
                    if (lineStart >= 0)
                    {
                        int prevLineStart = content.LastIndexOf('\n', lineStart - 1) + 1;
                        int posInLine = cursorPos - lineStart - 1;
                        int prevLineLength = lineStart - prevLineStart;
                        cursorPos = prevLineStart + Math.Min(posInLine, prevLineLength);
                        UpdateCursorDisplay(content, cursorPos, editStartLine);
                    }
                }
                else if (keyInfo.Key == ConsoleKey.DownArrow)
                {
                    // Move cursor down one line
                    int currentLineStart = content.LastIndexOf('\n', cursorPos) + 1;
                    int currentLineEnd = content.IndexOf('\n', currentLineStart);
                    
                    if (currentLineEnd >= 0 && currentLineEnd + 1 < content.Length)
                    {
                        int nextLineStart = currentLineEnd + 1;
                        int nextLineEnd = content.IndexOf('\n', nextLineStart);
                        if (nextLineEnd == -1) nextLineEnd = content.Length;
                        
                        int posInLine = cursorPos - currentLineStart;
                        int nextLineLength = nextLineEnd - nextLineStart;
                        cursorPos = nextLineStart + Math.Min(posInLine, nextLineLength);
                        UpdateCursorDisplay(content, cursorPos, editStartLine);
                    }
                }
                else if (keyInfo.Key == ConsoleKey.Home)
                {
                    // Move to start of line
                    int lineStart = content.LastIndexOf('\n', cursorPos) + 1;
                    cursorPos = lineStart;
                    UpdateCursorDisplay(content, cursorPos, editStartLine);
                }
                else if (keyInfo.Key == ConsoleKey.End)
                {
                    // Move to end of line
                    int lineEnd = content.IndexOf('\n', cursorPos);
                    if (lineEnd == -1) lineEnd = content.Length;
                    cursorPos = lineEnd;
                    UpdateCursorDisplay(content, cursorPos, editStartLine);
                }
                else if (keyInfo.Key == ConsoleKey.Enter)
                {
                    content = content.Insert(cursorPos, "\n");
                    cursorPos++;
                    Console.ForegroundColor = ConsoleColor.White;
                    RedrawContent(content, cursorPos, editStartLine);
                }
                else if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    if (cursorPos > 0)
                    {
                        content = content.Remove(cursorPos - 1, 1);
                        cursorPos--;
                        Console.ForegroundColor = ConsoleColor.White;
                        RedrawContent(content, cursorPos, editStartLine);
                    }
                }
                else if (keyInfo.Key == ConsoleKey.Delete)
                {
                    if (cursorPos < content.Length)
                    {
                        content = content.Remove(cursorPos, 1);
                        Console.ForegroundColor = ConsoleColor.White;
                        RedrawContent(content, cursorPos, editStartLine);
                    }
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    content = content.Insert(cursorPos, keyInfo.KeyChar.ToString());
                    cursorPos++;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(keyInfo.KeyChar);
                    UpdateCursorDisplay(content, cursorPos, editStartLine);
                }
            }
            
            // Show decision menu
            bool save = ShowDecisionMenu("\nSave changes?\n", "Save", "Cancel");

            if (save)
            {
                noteToEdit.Content = content;
                File.WriteAllText(Path.Combine(notesFolder, $"{noteToEdit.Title}.txt"), content);
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("\n\nChanges saved! Press any key to exit...");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("\n\nChanges discarded. Press any key to go exit...");
            }
            Console.ReadKey(true);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Invalid selection. Press any key to exit...");
            Console.ReadKey(true);
        }
    }

    void UpdateCursorDisplay(string content, int cursorPos, int editStartLine, bool isCreate = false)
    {
        Console.ForegroundColor = ConsoleColor.White;
        
        // Calculate where the cursor should be
        int lineCount = 0;
        int lastNewlinePos = 0;
        
        for (int i = 0; i < cursorPos; i++)
        {
            if (content[i] == '\n')
            {
                lineCount++;
                lastNewlinePos = i + 1;
            }
        }
        
        int posInLine = cursorPos - lastNewlinePos;
        int cursorLine = editStartLine + lineCount;
        int cursorCol = posInLine;
        
        // Add offset for ">" prompt on first line
        if (isCreate && lineCount == 0)
        {
            cursorCol += 2;
        }
        
        Console.ForegroundColor = ConsoleColor.White;
        Console.SetCursorPosition(cursorCol, cursorLine);
        Console.ForegroundColor = ConsoleColor.White;
    }

    void RedrawContent(string content, int cursorPos, int editStartLine, bool isCreate = false)
    {
        Console.ForegroundColor = ConsoleColor.White;
        
        // Clear everything from edit start
        for (int i = 0; i < Console.BufferHeight - editStartLine; i++)
        {
            Console.SetCursorPosition(0, editStartLine + i);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(new string(' ', Console.WindowWidth));
        }
        
        // Redraw content
        Console.SetCursorPosition(0, editStartLine);
        Console.ForegroundColor = ConsoleColor.White;
        
        if (isCreate)
        {
            Console.Write("> ");
        }
        
        for (int i = 0; i < content.Length; i++)
        {
            Console.ForegroundColor = ConsoleColor.White;
            if (content[i] == '\n')
            {
                Console.WriteLine();
                if (isCreate) Console.Write("> ");
            }
            else
            {
                Console.Write(content[i]);
            }
        }
        
        Console.ForegroundColor = ConsoleColor.White;
        
        // Update cursor position
        UpdateCursorDisplay(content, cursorPos, editStartLine, isCreate);
    }
    /// <summary>
    /// This feature allows users to delete whatever note they want.
    /// </summary>
    void DeleteNote()
    {
        if (notes.Count == 0)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("No notes found to delete. Press any key to exit...");
            Console.ReadKey(true);
            return;
        }

        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Select which note number to delete:\n"); //The user can select which note to delete
        
        Console.ForegroundColor = ConsoleColor.White;
        for (int i = 0; i < notes.Count; i++)
            Console.WriteLine($"{i + 1}. {notes[i].Title}");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("\n> ");

        if (!int.TryParse(Console.ReadLine(), out int choice) || choice <= 0 || choice > notes.Count)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Invalid choice. Press any key to return...");
            Console.ReadKey(true);
            return;
        }

        var note = notes[choice - 1];

        // Confirmation Menu (Uses same Green/Red logic)
        // (Dark Red and Dark Green pag may action na pagpipilian si user tas normal red and green naman pag wala :p)
        bool confirm = ShowDecisionMenu($"Are you sure you want to delete '{note.Title}'?\n\n", "Yes", "No");

        if (confirm)
        {
            string path = Path.Combine(notesFolder, $"{note.Title}.txt");
            if (File.Exists(path)) File.Delete(path);
            notes.RemoveAt(choice - 1);
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("\n\nNote deleted successfully.");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("\n\nDeletion cancelled.");
        }
        
        Console.ReadKey(true);
    }

    void ShowCalendar()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.White;

        Console.WriteLine("==============================================");
        Console.WriteLine("=            CALENDAR AND SCHEDULING         =");
        Console.WriteLine("==============================================\n");

        Console.WriteLine("(Under construction :p)\n");
        Console.WriteLine("Press any key to return to the main menu...");
        Console.ReadKey(true);
    }

    // dito casuco ayusin mo nag ka letche letche ka stress, ako na modify tas delete hehe
    
    static void Main()
    {
        TotePadApp app = new TotePadApp();
        app.StartupSequence();
        app.LoadNotes();
        app.ShowMainMenu();
    }
}