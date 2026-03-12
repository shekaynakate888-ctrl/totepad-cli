using System.Text;
namespace Totepad;

// 1. Models 
/// <summary>
/// these are the basic data structures for our notes and events. A Note has a Title and Content, while an Event has a Title, Date, Time, and Description. Ginamit ko ang C# 9 record type para sa mga ito kasi simple lang sila at gusto ko yung built-in immutability at value-based equality na meron sila.
/// </summary>
public record Note(string Title, string Content);
public record Event(string Title, DateTime Date, string Time, string Description);

public static class TotepadConstants
{
    public const string NotesFolder = "Notes";
    public const string eventsFolder = "Events";
    public const string NoteExtension = ".txt";
    public const string eventsFile = "events.txt";

    public const ConsoleColor PrimaryColor = ConsoleColor.Yellow;
    public const ConsoleColor HighlightColor = ConsoleColor.Green;
    public const ConsoleColor ErrorColor = ConsoleColor.Red;
}

// 2. Note Services 
/// <summary>
/// this is where all the file handling happens.such as creating the notes directory, loading and saving notes, and also loading and saving events. 
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
                string rawContent = File.ReadAllText(file);
                string normalizedContent = rawContent.Replace("\r\n", "\n").Replace("\r", "\n");
                notes.Add(new Note(
                    Path.GetFileNameWithoutExtension(file),
                    normalizedContent
                ));
            }
        }
        catch (IOException ex) // sinasalo kapag may error sa pag-read ng files
        {
            MenuRenderer.ShowErrorMessage($"Error loading notes: {ex.Message}");
        }
        return notes;
    }

    public List<Event> LoadAllEvents()
    {
        var events = new List<Event>();
        string filePath = Path.Combine(TotepadConstants.eventsFolder, TotepadConstants.eventsFile);

        try
        {
            if (File.Exists(filePath))
            {
                foreach (string line in File.ReadAllLines(filePath))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split('|');
                    if (parts.Length == 4 && DateTime.TryParse(parts[0], out DateTime parsedDate))
                    {
                        events.Add(new Event(parts[2], parsedDate, parts[1], parts[3]));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MenuRenderer.ShowErrorMessage($"Could not load events: {ex.Message}");
        }
        return events;
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

    public void SaveEvents(List<Event> events)
    {
        // Uses the constants we just defined
        string folder = TotepadConstants.eventsFolder;
        string filePath = Path.Combine(folder, TotepadConstants.eventsFile);

        try
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var eventLines = events.Select(e => $"{e.Date:yyyy-MM-dd}|{e.Time}|{e.Title}|{e.Description}");
            File.WriteAllLines(filePath, eventLines);
        }
        catch (IOException ex)
        {
            MenuRenderer.ShowErrorMessage($"File Error: Could not save events. {ex.Message}");
        }
        catch (Exception ex)
        {
            MenuRenderer.ShowErrorMessage($"An unexpected error occurred: {ex.Message}");
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
/// this is responsible for all the visual stuff in the console. It has methods to draw the main menu, the calendar grid, and also some helper methods for showing error messages and decision prompts. 
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

    public static void DrawCalendarGrid(DateTime viewDate, List<Event> events)
    {   
        // Set the highlight color for the calendar grid
        Console.ForegroundColor = TotepadConstants.HighlightColor;
        DateTime firstDay = new DateTime(viewDate.Year, viewDate.Month, 1);
        int daysInMonth = DateTime.DaysInMonth(viewDate.Year, viewDate.Month);
        int dayOfWeek = (int)firstDay.DayOfWeek;
        // Calendar header
        string divider = "============================================"; 
        Console.WriteLine(divider);
        
        string monthYear = viewDate.ToString("MMMM yyyy");
        int monthPadding = (42 - monthYear.Length - 2) / 2;
        Console.WriteLine("=" + new string(' ', monthPadding) + monthYear + new string(' ', 42 - monthYear.Length - monthPadding - 2) + "=");
        // Days of week header
        Console.WriteLine(divider);
        Console.WriteLine("=  S  =  M  =  T  =  W  =  T  =  F  =  S  =");
        Console.WriteLine(divider);
        // We start with the first day of the month, so we need to add empty spaces for the days before it
        int currentColumn = 0;
        Console.Write("=");
        // Add empty spaces for days before the first day of the month
        for (int i = 0; i < dayOfWeek; i++)
        {
            Console.Write("     ="); 
            currentColumn++;
        }
        // Now we print the days of the month, and we check if any of them should be highlighted (current day or event day)
        for (int day = 1; day <= daysInMonth; day++)
        {
            // highlight event logic starts here
            DateTime dateToCheck = new DateTime(viewDate.Year, viewDate.Month, day);
            if (dateToCheck.Date == DateTime.Today)
                Console.ForegroundColor = ConsoleColor.Cyan; // Highlight current day in cyan
            else if (events.Any(e => e.Date.Date == dateToCheck.Date)) // Highlight days with events in magenta
                Console.ForegroundColor = ConsoleColor.Magenta;
            else
                Console.ForegroundColor = TotepadConstants.HighlightColor;

            Console.Write($"  {day,2} ");
            Console.ForegroundColor = TotepadConstants.HighlightColor;
            Console.Write("=");
            currentColumn++;
            // After printing each day, check if we need to move to the next line
            if (currentColumn % 7 == 0 && day < daysInMonth)
            {
                Console.WriteLine();
                Console.Write("=");
            }
        }
        // Fill the remaining cells in the last week with empty spaces
        while (currentColumn % 7 != 0)
        {
            Console.Write("     =");
            currentColumn++;
        }
        Console.WriteLine("\n" + divider);
    }
    public static void InstructionHeader(string message)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;

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
            else if (key == ConsoleKey.Escape) return -1;
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
/// this is a custom text editor that runs in the console. It allows users to type and edit multi-line content with basic navigation (arrow keys), editing (backspace, delete), and saving (tab key). It also handles rendering the text and keeping the cursor in the right position.
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
            if (keyInfo.Key == ConsoleKey.Escape) return null;
            
            if (keyInfo.Key == ConsoleKey.LeftArrow) cursorPos = Math.Max(0, cursorPos - 1);
            else if (keyInfo.Key == ConsoleKey.RightArrow) cursorPos = Math.Min(sb.Length, cursorPos + 1);
            
            else if (keyInfo.Key == ConsoleKey.UpArrow) cursorPos = MoveVertical(sb.ToString(), cursorPos, -1);
            else if (keyInfo.Key == ConsoleKey.DownArrow) cursorPos = MoveVertical(sb.ToString(), cursorPos, 1);

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

    private int MoveVertical(string text, int currentPos, int direction)
    {
        string[] lines = text.Split('\n');
        int currentLineIndex = 0;
        int currentColumn = 0;
        int tempPos = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            if (currentPos <= tempPos + lines[i].Length)
            {
                currentLineIndex = i;
                currentColumn = currentPos - tempPos;
                break;
            }
            tempPos += lines[i].Length + 1; 
        }

        int targetLineIndex = currentLineIndex + direction;

        if (targetLineIndex < 0 || targetLineIndex >= lines.Length) 
            return currentPos;

        int newPos = 0;
        for (int i = 0; i < targetLineIndex; i++)
        {
            newPos += lines[i].Length + 1;
        }

        newPos += Math.Min(currentColumn, lines[targetLineIndex].Length);
        
        return newPos;
    }

    private void Render(StringBuilder sb, int cursorPos, int startLine, bool isCreate)
    {
        // Move back to where the typing area starts
        Console.SetCursorPosition(0, startLine);
    
        // Clear a large enough area so old text doesn't "ghost"
        for (int i = 0; i < 30; i++)
        {
            Console.Write(new string(' ', Console.WindowWidth));
        }
    
        Console.SetCursorPosition(0, startLine);
        Console.ForegroundColor = ConsoleColor.White;

        string content = sb.ToString();

        // If creating, we ensure every NEW line starts with the prompt
        if (isCreate)
        {
            // This splits the text and re-adds the prompt to every visual line
            Console.Write("> " + content.Replace("\n", "\n> "));
        }
        else
        {
            Console.Write(content);
        }   
    
        UpdateCursor(sb, cursorPos, startLine, isCreate);
    }
    private void UpdateCursor(StringBuilder sb, int cursorPos, int startLine, bool isCreate)
    {
        int width = Console.WindowWidth;
        int currentX = isCreate ? 2 : 0; 
        int currentY = startLine;

        string textToCursor = sb.ToString().Substring(0, cursorPos);

        foreach (char c in textToCursor)
        {
            if (c == '\n')
            {
                currentX = isCreate ? 2 : 0;
                currentY++;
            }
            else
            {
                currentX++;
                if (currentX >= width)
                {
                    currentX = 0; 
                    currentY++;
                }         
            }
        }
        int safeX = Math.Clamp(currentX, 0, width - 1);
        int safeY = Math.Clamp(currentY, 0, Console.BufferHeight - 1);

        Console.SetCursorPosition(safeX, safeY);
    }
    
}
// 5. Main Application
/// <summary>
/// this is the main class that runs the application. It initializes the NoteService, loads existing notes and events, and manages the main loop for the user interface. It has methods for displaying the notes menu and calendar menu, as well as handling the logic for creating, viewing, modifying, and deleting notes and events.
/// </summary>

class TotePad
{
    private NoteService _service = new();
    private List<Note> _notes = new();
    private NoteEditor _editor = new();
    private List<Event> _events = new List<Event>();

    public void Run()
    {
        _service.EnsureDirectoryExists();
        _notes = _service.LoadAllNotes();
       
        
        while (true)
        {
            MenuRenderer.DrawHeader("TOTEPAD MAIN MENU");
            int choice = MenuRenderer.ShowArrowMenu(new[] { "Notes", "Calendar", "Exit" });

            if (choice == 0) NotesMenu();
            else if (choice == 1) CalendarMenu();
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
            MenuRenderer.InstructionHeader("Use arrow keys to navigate, Enter to select, or Esc to go back.");
            int action = MenuRenderer.ShowArrowMenu(new[] { "Create", "View", "Modify", "Delete", "Back" });
            if (action == -1 || action == 4) break;
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
        MenuRenderer.InstructionHeader("Enter Title or press ESC to cancel.");
    
        Console.CursorVisible = true; 
        Console.ForegroundColor = ConsoleColor.Cyan; 
        Console.Write("Title: "); 

        string title = "";
        while (true)
        {
            var keyInfo = Console.ReadKey(true);
            if (keyInfo.Key == ConsoleKey.Escape) return; // Immediate exit to menu
            if (keyInfo.Key == ConsoleKey.Enter && !string.IsNullOrEmpty(title)) break;
            if (keyInfo.Key == ConsoleKey.Backspace && title.Length > 0)
            {
                title = title.Remove(title.Length - 1);
                Console.Write("\b \b");
            }
           else if (!char.IsControl(keyInfo.KeyChar) && keyInfo.KeyChar != '|') 
            {
                title += keyInfo.KeyChar;
                Console.Write(keyInfo.KeyChar);
            }
        }
        title = title.Trim(); 

        if (_notes.Any(n => n.Title.Equals(title, StringComparison.OrdinalIgnoreCase))) 
        {
            MenuRenderer.ShowErrorMessage("Title already exists!"); 
            return; 
        }   

        string? content = _editor.EditContent("", true); 
        if (content == null) return; 

        if (MenuRenderer.ShowDecisionMenu("Save this note?", "Discard", "Save")) 
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
        MenuRenderer.InstructionHeader("Use arrow keys to select a note. Press Enter to view, or Esc to cancel");    
        int index = MenuRenderer.ShowArrowMenu(_notes.Select(n => n.Title).ToArray());
        if (index == -1) return;
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
        MenuRenderer.InstructionHeader("Use arrow keys to select a note. Press Enter to edit, or Esc to cancel");
        int index = MenuRenderer.ShowArrowMenu(_notes.Select(n => n.Title).ToArray());
        if (index == -1) return;

        string? newContent = _editor.EditContent(_notes[index].Content, false);
        if (newContent == null) return;
        
        if (MenuRenderer.ShowDecisionMenu("Save changes?", "Don't Save", "Save"))
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
        MenuRenderer.InstructionHeader("Use arrow keys to select a note. Press Enter to delete, or Esc to cancel");
        int index = MenuRenderer.ShowArrowMenu(_notes.Select(n => n.Title).ToArray());
        if (index == -1) return;
        if (MenuRenderer.ShowDecisionMenu("Save changes?", "Don't Save", "Save"))
        {
            _service.DeleteNote(_notes[index].Title);
            _notes.RemoveAt(index);
        }
    }

/// Calendar part
    void CalendarMenu()
    {
        DateTime viewDate = DateTime.Now;
        while (true)
        {
            _events = _service.LoadAllEvents();
            MenuRenderer.DrawHeader("CALENDAR & EVENTS");
            MenuRenderer.InstructionHeader("Cyan for current day, Magenta for days with events");
            MenuRenderer.DrawCalendarGrid(viewDate, _events);
            Console.WriteLine($"\n--- Events for {viewDate:MMMM yyyy} ---");
            var monthEvents = _events.Where(e => e.Date.Month == viewDate.Month && e.Date.Year == viewDate.Year).ToList();

            if (!monthEvents.Any()) Console.WriteLine(" (No events scheduled) ");
            else monthEvents.ForEach(e => Console.WriteLine($" • {e.Date:dd}: {e.Title} ({e.Time})"));

            MenuRenderer.InstructionHeader(" [←]|[→] Change Month | [Enter] Select Action | [Esc] Back");
            var key = Console.ReadKey(true).Key;
            if (key == ConsoleKey.RightArrow) viewDate = viewDate.AddMonths(1);
            else if (key == ConsoleKey.LeftArrow) viewDate = viewDate.AddMonths(-1);
            else if (key == ConsoleKey.Escape) break;
            else if (key == ConsoleKey.Enter)
            {
                int action = MenuRenderer.ShowArrowMenu(new[] { "Create Event", "View event", "Modify Event", "Delete Event", "Cancel" });
                if (action == 0) CreateEvent();
                else if (action == 1) ViewEvent();
                else if (action == 2) ModifyEvent();
                else if (action == 3) DeleteEvent();
            }
        }
    }
   
    // This allows users to create new events by filling in the details like title, date, time, and description. 
    void CreateEvent()
    {
        Console.Clear();
        MenuRenderer.DrawHeader("SCHEDULE NEW EVENT");
        MenuRenderer.InstructionHeader("Fill in the details. Press ESC at any time to cancel.");

        string title;
        while (true)
        {
            if (!TryGetInput(" Event Title: ", out title)) return;
            if (!string.IsNullOrWhiteSpace(title)) break;
            MenuRenderer.ShowErrorMessage("Title cannot be blank!");
            // Redraw header because ShowErrorMessage destro up alignment
            MenuRenderer.DrawHeader("SCHEDULE NEW EVENT");
            MenuRenderer.InstructionHeader("Fill in the details, Press ESC at any time to cancel");
        }
        DateTime eventDate;
        while (true)
        {
            if (!TryGetInput(" Date (YYYY-MM-DD): ", out string dateInput)) return;

            if (DateTime.TryParse(dateInput, out eventDate))
            {
                break;
            }
            MenuRenderer.ShowErrorMessage("Invalid Date! Please use YYYY-MM-DD (e.g., 2026-12-25)");
            MenuRenderer.DrawHeader("SCHEDULE NEW EVENT");
            MenuRenderer.InstructionHeader("Fill in the details, Press ESC at any time to cancel");
        }

        string time;
        while (true)
        {
            if (!TryGetInput(" Time (HH:mm): ", out time)) return;
            if (!string.IsNullOrWhiteSpace(time)) break;
            MenuRenderer.ShowErrorMessage("Time cannot be blank!");
            MenuRenderer.DrawHeader("SCHEDULE NEW EVENT");
            MenuRenderer.InstructionHeader("Fill in the details, Press ESC at any time to cancel");
        }

        string description;
        while (true)
        {
            if (!TryGetInput("Description: ", out description)) return;
            if (!string.IsNullOrWhiteSpace(description)) break;
            MenuRenderer.ShowErrorMessage("Description cannot be blank!");
            MenuRenderer.DrawHeader("SCHEDULE NEW EVENT");
            MenuRenderer.InstructionHeader("Fill in the details, Press ESC at any time to cancel");
        }
        // Create and Save
        Event newEvent = new Event(title, eventDate, time, description);
        _events.Add(newEvent);
        _service.SaveEvents(_events);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n [ Event Saved! ]");
        Thread.Sleep(800);
    }
    //This allows users to view details of existing events by selecting from the calendar.
    void ViewEvent()
    {
        if (!_events.Any())
        {
            MenuRenderer.ShowErrorMessage("NO EVENTS!");
            return;
        }
        MenuRenderer.DrawHeader("VIEW EVENTS");
        MenuRenderer.InstructionHeader("Use arrow keys to select an event. Press Enter to view, or Esc to cancel");
        int index = MenuRenderer.ShowArrowMenu(_events.Select(e => $"{e?.Title ?? "No Title"} ({e?.Date:MMM dd, yyyy})").ToArray());
        if (index == -1 || _events[index] == null) return;
        var selectedEvent = _events[index];
        MenuRenderer.DrawHeader(selectedEvent.Title ?? "(No Title)");
        Console.WriteLine($"Date: {selectedEvent.Date:yyyy-MM-dd}");
        Console.WriteLine($"Time: {selectedEvent.Time ?? "(No Time)"}\n");
        Console.WriteLine(selectedEvent.Description ?? "(No Description)");
        Console.WriteLine("\n\n(Press any key to return)");
        Console.ReadKey(true);
    }
    // This allows users to modify existing events, changing any of the details as needed.
    void ModifyEvent()
    {
        if (!_events.Any())
        {
            MenuRenderer.ShowErrorMessage("NO EVENTS TO MODIFY!");
            return;
        }
        MenuRenderer.DrawHeader("SELECT EVENTS TO MODIFY");
        MenuRenderer.InstructionHeader("Use arrow keys to select an event. Press Enter to edit, or Esc to cancel");
        int index = MenuRenderer.ShowArrowMenu(_events.Select(e => $"{e.Title} ({e.Date:MMM dd, yyyy})").ToArray());
        if (index == -1) return;
        Event selectedEvent = _events[index];
        // changes to title
        if (!TryGetInput($" Title ({selectedEvent.Title}): ", out string title)) return;
        if (string.IsNullOrWhiteSpace(title)) title = selectedEvent.Title;
        // changes to date, default to original date if parsing fails
        if (!TryGetInput($" Date ({selectedEvent.Date:yyyy-MM-dd}): ", out string dateInput)) return;
        DateTime eventDate = DateTime.TryParse(dateInput, out var d) ? d : selectedEvent.Date;
        // changes to time, default to original time if input is empty
        if (!TryGetInput($" Time ({selectedEvent.Time}): ", out string time)) return;
        if (string.IsNullOrWhiteSpace(time)) time = selectedEvent.Time;

        Console.WriteLine("\n Description (TAB to save, ESC to cancel):");
        NoteEditor editor = new NoteEditor();
        string? description = editor.EditContent(selectedEvent.Description ?? "", false);

        if (description == null) return;
        if (MenuRenderer.ShowDecisionMenu("Save changes?", "Don't Save", "Save"))
        {
            _events[index] = selectedEvent with { Title = title, Date = eventDate, Time = time, Description = description };
            _service.SaveEvents(_events);
        }
    }
    // This allows users to delete events they no longer need, with a confirmation step to prevent accidental deletions.
    void DeleteEvent()
    {
        if (!_events.Any())
        {
            MenuRenderer.ShowErrorMessage("NO EVENTS TO DELETE!");
            return;
        }
        MenuRenderer.DrawHeader("SELECT EVENT TO DELETE");
        MenuRenderer.InstructionHeader("Use arrow keys to select an event. Press Enter to delete, or Esc to cancel");
        int index = MenuRenderer.ShowArrowMenu(_events.Select(e => $"{e.Title} ({e.Date:MMM dd, yyyy})").ToArray());
        if (index == -1) return;
        if (MenuRenderer.ShowDecisionMenu($"Delete '{_events[index].Title}'?", "Cancel", "Delete"))
        {
            _events.RemoveAt(index);
            _service.SaveEvents(_events);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n [ Event has been removed ]");
            Thread.Sleep(800);
        }
    }
    // Helper method to for cancelling (ESC) purpose (as well as title forground color) sepcifically for CreateEvent since it is not in loop,
    private bool TryGetInput(string prompt, out string result)
    {
        Console.CursorVisible = true;
        Console.ResetColor();
        Console.Write(prompt);
        //cyan color for creating event title
        Console.ForegroundColor = ConsoleColor.Cyan;
        result = "";
        while (true)
        {
            var keyInfo = Console.ReadKey(true);
            if (keyInfo.Key == ConsoleKey.Escape) return false; 
            if (keyInfo.Key == ConsoleKey.Enter) { Console.WriteLine(); return true; }

            if (keyInfo.Key == ConsoleKey.Backspace && result.Length > 0)
            {
                result = result.Remove(result.Length - 1);
                Console.Write("\b \b");
            }
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                // Prevent '|' character to avoid issues with event saving format
                if (keyInfo.KeyChar != '|')
                {
                    result += keyInfo.KeyChar;
                    Console.Write(keyInfo.KeyChar);
                }
               
            }
        }
    }
    static void Main() => new TotePad().Run();
}