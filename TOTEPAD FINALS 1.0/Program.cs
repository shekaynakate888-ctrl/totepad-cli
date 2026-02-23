using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
namespace Totepad;

// 1. Models 
/// <summary>
/// Ito ang pattern para sa mga notes natin. May Title ito para sa pangalan ng file at Content para sa mismong text. Ginawa ko itong 'record' para hindi basta-basta magulo ang data at madaling i-manage. Dito umiikot ang buong app para sa paggawa, pag-edit, at pag-delete ng notes kemkemekeme
/// </summary>
public record Note(string Title, string Content);

public static class TotepadConstants
{
    public const string NotesFolder = "Notes";
    public const string NoteExtension = ".txt";
    public const ConsoleColor PrimaryColor = ConsoleColor.Yellow;
    public const ConsoleColor HighlightColor = ConsoleColor.Green;
    public const ConsoleColor ErrorColor = ConsoleColor.Red;
}



// 2. Note Services 
/// <summary>
/// Ito ang taga-handle ng lahat ng file settings sa notes natin. Siya ang sumisigurado na may folder para sa notes, siya ang nagse-save, naglo-load, at nagbubura ng mga files. Nilagay ko lahat ng file logic dito para hindi magulo sa main program at may kasama na rin itong error handling para hindi basta-basta mag-crash ang app kung may problema sa folder.
/// </summary>
public class NoteService
{
    public void EnsureDirectoryExists()
    {
        try
        {
            if (!Directory.Exists(TotepadConstants.NotesFolder))
                Directory.CreateDirectory(TotepadConstants.NotesFolder);
        }
        catch (Exception ex) // sinasalo kapag may error sa pag-create ng folder
        {
            Console.WriteLine($"Critical Error: Could not create directory. {ex.Message}");
        }
    }

    public List<Note> LoadAllNotes()
    {
        var notes = new List<Note>();
        try
        {
            foreach (string file in Directory.GetFiles(TotepadConstants.NotesFolder, $"*{TotepadConstants.NoteExtension}"))
            {
                notes.Add(new Note(
                    Path.GetFileNameWithoutExtension(file),
                    File.ReadAllText(file)
                ));
            }
        }
        catch (IOException ex) // sinasalo kapag may error sa pag-read ng files
        {
            MenuRenderer.ShowErrorMessage($"Error loading notes: {ex.Message}");
        }
        return notes;
    }


    public bool SaveNote(Note note)
    {
        try
        {
            string safeTitle = MenuRenderer.SanitizeFilename(note.Title);
            string path = Path.Combine(TotepadConstants.NotesFolder, safeTitle + TotepadConstants.NoteExtension);
            File.WriteAllText(path, note.Content);
            return true;
        }
        catch (Exception ex) // sinasalo kapag may error sa pag-save ng file
        {
            MenuRenderer.ShowErrorMessage($"Failed to save: {ex.Message}");
            return false;
        }
    }

    public void DeleteNote(string title)
    {
        try
        {
            string path = Path.Combine(TotepadConstants.NotesFolder, title + TotepadConstants.NoteExtension);
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception ex) // sinasalo kapag may error sa pag-delete ng file
        {
            MenuRenderer.ShowErrorMessage($"Delete failed: {ex.Message}");
        }
    }
}

// 3. UI 
/// <summary>
/// Dito ko nilagay lahat ng code para sa itsura ng app, gaya ng mga header, instruction boxes, at mga menu. Ginawa ko itong helper para isang lugar na lang ang babaguhin ko kung gusto kong palitan ang design at para hindi masyadong mahaba o magulo yung main code 
/// </summary>
public static class MenuRenderer
{
    public static void DrawHeader(string title)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Yellow;

        int boxWidth = 42; 
        int leftPadding = (boxWidth - title.Length) / 2;
        int rightPadding = boxWidth - title.Length - leftPadding;
    
        Console.WriteLine("==============================================");
        Console.WriteLine("=                                            =");
        Console.WriteLine($"= {new string(' ', leftPadding)}{title}{new string(' ', rightPadding)} =");
        Console.WriteLine("=                                            =");
        Console.WriteLine("==============================================\n");
    }

    public static void InstructionHeader(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;

        int boxWidth = 26; 
        int leftPadding = Math.Max(0, (boxWidth - message.Length) / 2);
        int rightPadding = Math.Max(0, boxWidth - message.Length - leftPadding);

        Console.WriteLine($"~ {message} ~\n");
        Console.ForegroundColor = ConsoleColor.White;
    }
    public static string SanitizeFilename(string title)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            title = title.Replace(c, '_');
        return title;
    }

    public static void ShowErrorMessage(string msg)
    {
        Console.ForegroundColor = TotepadConstants.ErrorColor;
        Console.WriteLine($"\n[ERROR] {msg}");
        Console.ForegroundColor = ConsoleColor.White;
        Thread.Sleep(2000);
    }

    public static int ShowArrowMenu(string[] options)
    {
        int startLine = Console.CursorTop;
        if (startLine + options.Length >= Console.BufferHeight)
        {
            Console.Clear(); // Linisin ang screen kung wala nang space para sa menu
            DrawHeader(" NOTES MENU ");
            startLine = Console.CursorTop;
        }
        int selected = 0;
        Console.CursorVisible = false;
        while (true)
        {
            for (int i = 0; i < options.Length; i++)
            {
                Console.SetCursorPosition(0, startLine + i);
                if (i == selected)
                {
                    Console.ForegroundColor = TotepadConstants.HighlightColor;
                    Console.WriteLine($"> [ {options[i]} ]  ");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"    {options[i]}    ");
                }
            }

            var key = Console.ReadKey(true).Key;
            if (key == ConsoleKey.UpArrow) selected = (selected - 1 + options.Length) % options.Length;
            else if (key == ConsoleKey.DownArrow) selected = (selected + 1) % options.Length;
            else if (key == ConsoleKey.Enter) return selected;
        }
    }

    public static bool ShowDecisionMenu(string prompt, string leftOption, string rightOption)
    {
        int selected = 0; // 0 = Left (Cancel/No), 1 = Right (Save/Yes) 
        string[] options = { leftOption, rightOption };
        
        Console.WriteLine($"\n{prompt}");
        int menuTop = Console.CursorTop;
        Console.CursorVisible = false;

        while (true)
        {
            Console.SetCursorPosition(0, menuTop);
            // Clear the line before drawing buttons
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, menuTop);

            for (int i = 0; i < options.Length; i++)
            {
                if (i == selected)
                {
                    // Left is Red (Cancel), Right is Green (Save), 
                    Console.ForegroundColor = (i == 0) ? ConsoleColor.Red : ConsoleColor.Green;
                    Console.Write($"[ {options[i]} ]     ");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write($"  {options[i]}       ");
                }
            }

            var key = Console.ReadKey(true).Key;
            if (key == ConsoleKey.LeftArrow) selected = 0;
            else if (key == ConsoleKey.RightArrow) selected = 1;
            else if (key == ConsoleKey.Enter)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.CursorVisible = true; 
                return selected == 1; // Returns True only if "Save" is selected
            }
        }
    }
}

// 4. Note Editor 
/// <summary>
/// Dito nangyayari yung pag-type at pag-edit ng notes. Pwede mo imove gamit ang arrow keys at gumamit ng backspace o delete para magbura. Lagi nitong pinapakita yung cursor mo habang nagsusulat ka, at kapag tapos ka na, pindutin lang ang TAB key para ma-save o lumabas
/// </summary>

public class NoteEditor
{
    public string EditContent(string initialContent, bool isCreate)
    {
        Console.CursorVisible = true;
        StringBuilder sb = new StringBuilder(initialContent);
        int cursorPos = sb.Length;
        int editStartLine = Console.CursorTop;

        Render(sb, cursorPos, editStartLine, isCreate);

        while (true)
        {
            ConsoleKeyInfo keyInfo = Console.ReadKey(true);

            if (keyInfo.Key == ConsoleKey.Tab) break;

            if (keyInfo.Key == ConsoleKey.LeftArrow) cursorPos = Math.Max(0, cursorPos - 1);
            else if (keyInfo.Key == ConsoleKey.RightArrow) cursorPos = Math.Min(sb.Length, cursorPos + 1);
            else if (keyInfo.Key == ConsoleKey.Backspace && cursorPos > 0)
            {
                sb.Remove(cursorPos - 1, 1);
                cursorPos--;
                Render(sb, cursorPos, editStartLine, isCreate);
            }
            else if (keyInfo.Key == ConsoleKey.Delete && cursorPos < sb.Length)
            {
                sb.Remove(cursorPos, 1);
                Render(sb, cursorPos, editStartLine, isCreate);
            }
            else if (keyInfo.Key == ConsoleKey.Enter)
            {
                sb.Insert(cursorPos, "\n");
                cursorPos++;
                Render(sb, cursorPos, editStartLine, isCreate);
            }
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                sb.Insert(cursorPos, keyInfo.KeyChar);
                cursorPos++;
                Render(sb, cursorPos, editStartLine, isCreate);
            }
            
            UpdateCursor(sb, cursorPos, editStartLine, isCreate);
        }

        return sb.ToString();
    }

    private void Render(StringBuilder sb, int cursorPos, int startLine, bool isCreate)
    {
        Console.SetCursorPosition(0, startLine);
        // Clear area
        for (int i = 0; i < 15; i++) Console.WriteLine(new string(' ', Console.WindowWidth));
        Console.SetCursorPosition(0, startLine);

        Console.ForegroundColor = ConsoleColor.White;
        string prompt = isCreate ? "> " : "";
        string content = sb.ToString();
        
        if (isCreate) Console.Write(prompt);
        Console.Write(content.Replace("\n", "\n" + prompt));
        
        UpdateCursor(sb, cursorPos, startLine, isCreate);
    }

    private void UpdateCursor(StringBuilder sb, int cursorPos, int startLine, bool isCreate)
    {
        string currentText = sb.ToString().Substring(0, cursorPos);
        int lines = currentText.Count(c => c == '\n');
        int lastNewLine = currentText.LastIndexOf('\n');
        int col = (cursorPos - (lastNewLine + 1)) + (isCreate ? 2 : 0);
        
        Console.SetCursorPosition(col, startLine + lines);
    }
}

// 5. Main Application
/// <summary>
/// Ito ang pinaka-utak ng app na nagpapatakbo sa lahat. Siya ang may hawak ng mga menu at calendar, at siya rin ang nag-uutos sa NoteService at NoteEditor para gumana sila. Hindi titigil ang app hangga’t hindi ka nag-e-exit, kaya pwede kang gumawa, tumingin, mag-ayos, o magbura ng notes kahit kailan mo gusto
/// </summary>

class TotePad
{
    private NoteService _service = new();
    private List<Note> _notes = new();
    private NoteEditor _editor = new();

    public void Run()
    {
        _service.EnsureDirectoryExists();
        _notes = _service.LoadAllNotes();
        
        while (true)
        {
            MenuRenderer.DrawHeader("TOTEPAD MAIN MENU");
            int choice = MenuRenderer.ShowArrowMenu(new[] { "Notes", "Calendar", "Exit" });

            if (choice == 0) NotesMenu();
            else if (choice == 1) ShowCalendar();
            else break;
        }
    }

    /// <summary>
    /// This displays the notes menu, allowing users to create, view, modify, or delete notes.
    /// </summary>
    void NotesMenu()
    {
        while (true)
        {
            MenuRenderer.DrawHeader("NOTES LIST");
            if (!_notes.Any()) Console.WriteLine("(No notes found)\n");
            else _notes.ForEach(n => Console.WriteLine($"- {n.Title}"));

            Console.WriteLine("\n--- Actions ---");
            int action = MenuRenderer.ShowArrowMenu(new[] { "Create", "View", "Modify", "Delete", "Back" });

            if (action == 0) CreateNote();
            else if (action == 1) ViewNote();
            else if (action == 2) ModifyNote();
            else if (action == 3) DeleteNote();
            else break;
        }
    }
    /// <summary>
    /// This feature allows users to create a new note with a title and content
    /// </summary>
    void CreateNote()
    {
        MenuRenderer.DrawHeader("CREATE NEW NOTE");
        MenuRenderer.InstructionHeader("Type your note content. Press TAB when done");
        Console.CursorVisible = true;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("Title: ");
        string title = Console.ReadLine()?.Trim() ?? "";
        Console.ForegroundColor = ConsoleColor.White;
        
        if (string.IsNullOrEmpty(title) || _notes.Any(n => n.Title.Equals(title, StringComparison.OrdinalIgnoreCase)))
        {
            MenuRenderer.ShowErrorMessage("Title empty or already exists!");
            return;
        }

        string content = _editor.EditContent("", true);
        if (MenuRenderer.ShowDecisionMenu("Save this note?", "Cancel", "Save"))
        {
            var newNote = new Note(title, content);
            if (_service.SaveNote(newNote)) _notes.Add(newNote);
        }
    }

    /// <summary>
    /// This feature allows users to view existing notes by selecting from their list.
    /// </summary>
    void ViewNote()
    {
        if (!_notes.Any()) return;
        MenuRenderer.DrawHeader("VIEW NOTE");
        MenuRenderer.InstructionHeader("Use arrow keys to select a note. Press Enter to view");    
        int index = MenuRenderer.ShowArrowMenu(_notes.Select(n => n.Title).ToArray());
        
        MenuRenderer.DrawHeader(_notes[index].Title);
        Console.WriteLine(_notes[index].Content);
        Console.WriteLine("\n\n(Press any key to return)");
        Console.ReadKey(true);
    }

    /// <summary>
    /// This feature allows users to modify existing notes.
    /// </summary>
    void ModifyNote()
    {
        if (!_notes.Any()) return;
        MenuRenderer.DrawHeader("SELECT NOTE TO MODIFY");
        MenuRenderer.InstructionHeader("Use arrow keys to select a note. Press Enter to edit");
        int index = MenuRenderer.ShowArrowMenu(_notes.Select(n => n.Title).ToArray());

        string newContent = _editor.EditContent(_notes[index].Content, false);
        if (MenuRenderer.ShowDecisionMenu("Save changes?", "Cancel", "Save"))
        {
            _notes[index] = _notes[index] with { Content = newContent };
            _service.SaveNote(_notes[index]);
        }
    }

    /// <summary>
    /// This feature allows users to delete whatever note they want.
    /// </summary>
    void DeleteNote()
    {
        if (!_notes.Any()) return;
        MenuRenderer.DrawHeader("SELECT NOTE TO DELETE");
        MenuRenderer.InstructionHeader("Use arrow keys to select a note. Press Enter to delete");
        int index = MenuRenderer.ShowArrowMenu(_notes.Select(n => n.Title).ToArray());

        if (MenuRenderer.ShowDecisionMenu($"Delete '{_notes[index].Title}'?", "Cancel", "Delete"))
        {
            _service.DeleteNote(_notes[index].Title);
            _notes.RemoveAt(index);
        }
    }

    void ShowCalendar()
    {
        MenuRenderer.DrawHeader("CALENDAR");
        MenuRenderer.InstructionHeader(" Press any key to return.");
        Console.WriteLine("UNDER CONSTRUVTION :PPPPPP)");
        Console.ReadKey(true);
    }

    static void Main() => new TotePad().Run();
}
