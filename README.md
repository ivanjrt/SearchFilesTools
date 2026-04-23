# 🔍 SearchFilesTool (Log Search)
<img width="802" height="637" alt="image" src="https://github.com/user-attachments/assets/22ddbd44-f05f-4466-a5d0-d305639370ee" />

A **Windows desktop application** for searching keywords across log files, text files, and Windows Event Logs. Built with WPF (.NET Framework 4.7.2) and licensed under the MIT License.

---

## 🎯 What This Program Does

Log Search lets you **drop a folder** (or a `.zip` archive) onto the window, type a keyword, and find every matching line across all supported files in that directory — recursively through all subfolders.

📁 **Supported file types:** `.log`, `.txt`, `.reg`, `.html`, `.json`, `.xml`

⚙️ **Additional capability:** Automatically discovers and converts Windows Event Log files (`.evtx`) into searchable `.xml` using the Windows `wevtutil` command, so event log contents become searchable too.

---

## ✨ Key Features

| Feature | Description |
|---------|-------------|
| 🖱️ **Drag-and-drop input** | Drop a folder or a `.zip` file onto the target area |
| 📦 **Automatic ZIP extraction** | Dropped `.zip` files are extracted to a sibling folder and searched |
| 🔎 **Case-insensitive keyword search** | Matches substrings across every line of every supported file |
| 📊 **Result grid** | Displays the matched sentence, full file path, and line number for each hit |
| 🖱️ **Double-click to open** | Double-click any result to open the source file in Notepad |
| 📋 **Ctrl+C to copy** | Copies the matched sentence text to the clipboard |
| ⏎ **Enter key to search** | Press `Enter` in the search box to trigger a search immediately |
| 🧹 **Clear button** | Clears the folder list, search text, and results in one click |
| 📈 **Progress indication** | Shows a progress bar during EVTX conversion and file search |
| 🔧 **EVTX-to-XML conversion** | Converts `.evtx` files to `.xml` so their event data can be searched like any other text file |
| ℹ️ **About dialog** | Shows build number, supported file types, and a liability disclaimer |
| ❓ **Help dialog** | Provides guidance on ZIP extraction troubleshooting |
| 🌙 **Dark theme UI** | Clean dark interface (`#121C27` background) designed for extended use |

---

## 🔒 Security Design: Nothing Is Saved or Transmitted

This application was designed with a **read-only, leave-no-trace** philosophy:

- 🚫 **No data is saved.** The application searches files in place and displays results in memory. It does not create databases, export files, or write search results anywhere on disk. Once you close the application, the results are gone.
- 🌐 **No network activity.** The application makes no network calls. It does not phone home, contact any server, or transmit any data externally.
- 📊 **No user data collection.** No telemetry, no analytics, no usage tracking.
- 💾 **No configuration files are written.** User preferences, search history, and file paths are not persisted between sessions. Every launch starts fresh.
- 📂 **Files are never modified.** The search engine reads files but never writes to them. Your source files remain untouched.
- 📝 **The only file the application creates is its own log file** (see below), which contains diagnostic information about the application itself — not the contents of your files.

---

## 🐛 How Logging Works When Something Breaks

The application includes a self-contained diagnostic logger that activates automatically on startup.

### 📁 Log file location

```
%APPDATA%\SearchFilesTool\Application.log
```

### 📋 What gets logged

- 🚀 **Application startup** — version, OS, architecture (32/64-bit), available memory
- 🔍 **Search operations** — when searches begin and complete
- ⚠️ **Errors and exceptions** — full exception type, message, stack trace, and inner exceptions

### 🔧 How it is created

1. On startup, `App.OnStartup()` calls `Logger.Initialize()` before anything else runs
2. The logger creates the `%APPDATA%\SearchFilesTool\` directory if it does not exist
3. It opens (or creates) `Application.log` and writes a startup header with environment details
4. From that point on, all `Logger.Info()`, `Logger.Warn()`, and `Logger.Error()` calls append timestamped entries to this file

### 🛡️ Three layers of crash protection

The application registers three global exception handlers in `App.xaml.cs` so that even unexpected crashes produce a log entry:

| Handler | Catches | Behavior |
|---------|---------|----------|
| `AppDomain.CurrentDomain.UnhandledException` | Crashes on non-UI threads | Logs the full exception. If the application is terminating, logs that fact as well. |
| `TaskScheduler.UnobservedTaskException` | Unobserved exceptions from async tasks | Logs the exception and marks it as observed to prevent the process from crashing. |
| `DispatcherUnhandledException` | Unhandled exceptions on the WPF UI thread | Logs the exception, shows a dialog to the user with the error message and the log file path, then marks it as handled so the application continues running. |

### 🧹 Automatic log cleanup

On every startup, the application deletes log files older than **7 days** via `Logger.CleanupOldLogs(7)`. This prevents the log from growing indefinitely.

### 🔒 Thread safety

All log writes are protected by a `lock` statement, so concurrent operations (search tasks, UI thread, exception handlers) can safely write to the same file without corruption.

---

## 🏗️ Architecture

```
App.xaml.cs          Logger.cs           evtxToXml.cs           MainWindow.xaml / .cs
(Startup + crash     (Diagnostics        (EVTX conversion)      (UI + search engine)
 guards)              logging)
     |                    |                      |                        |
     +-- OnStartup ------>+                      |                        |
          initializes     |                      |                        |
          logger,         |                      |                        |
          registers       |                      |                        |
          exception       |                      |                        |
          handlers        |                      |                        |
                            |                      |                  +-- LogResult (data model)
                            |                      |                  |   .Sentence
                            |                      |                  |   .Path
                            |                      |                  |   .LineNumber
                            |                      |                  |
                            |                      |                  +-- FolderListBox_Drop()
                            |                      |                  |   (drag-and-drop handler,
                            |                      |                  |    zip extraction)
                            |                      |                  |
                            |                      +<----- Search_Click() -----+
                            |                      |       (EVTX conversion    |
                            |                      |        then file search)  |
                            |                      |                           |
                            +<---------- Logger calls throughout -------------+
```

**Technology:** WPF (Windows Presentation Foundation) on .NET Framework 4.7.2, single-window architecture with code-behind (no MVVM framework).

---

## 📂 Project Structure

```
SearchFilesTools-master/
├── App.xaml                   # Application XAML definition
├── App.xaml.cs                # Startup logic & global exception handlers
├── Logger.cs                  # Thread-safe diagnostic logger
├── evtxToXml.cs               # EVTX-to-XML conversion via wevtutil
├── MainWindow.xaml            # UI layout (dark theme)
├── MainWindow.xaml.cs         # UI logic & keyword search engine
├── icon.png                   # Application icon (PNG)
├── iconLogo.ico               # Application icon (ICO)
├── App.config                 # .NET Framework 4.7.2 runtime config
├── packages.config            # NuGet dependencies
├── SearchFilesTool.csproj     # MSBuild project file
├── SearchFilesTool.sln        # Visual Studio solution file
├── Properties/
│   ├── AssemblyInfo.cs        # Assembly metadata (version, copyright)
│   ├── Resources.resx         # Resource definitions
│   └── Settings.settings      # User settings
├── .gitignore                 # Git ignore rules
├── .gitattributes             # Git line-ending normalization
├── LICENSE.txt                # MIT License
├── README.md                  # This file
└── BUGFIX_SUMMARY.md          # Bug fix documentation
```

---

## ⚙️ File Processing Workflow

Here is what happens from the moment you drop a folder to the moment results appear:

### 📥 Step 1: Input — Drag and Drop

You drag a folder or `.zip` file onto the drop zone (`FolderListBox`).

- **If a folder:** its path is stored in the ListBox.
- **If a `.zip`:** the application extracts it to a sibling directory using `System.IO.Compression.ZipFile.ExtractToDirectory()`. If that directory already exists, extraction is skipped. The resulting folder path is stored.

### 🔄 Step 2: EVTX Conversion (before search)

When you click **Search** (or press `Enter`), the application first scans the directory recursively for `.evtx` files.

For each `.evtx` file found (that does not already have a corresponding `.xml` file):

1. **Run `wevtutil`** — The Windows command-line tool `wevtutil query-events` is invoked to dump the event log as XML. Output is streamed to a temporary file (64KB buffer) to avoid loading everything into memory at once.
2. **Clean control characters** — The raw XML output is read line-by-line and stripped of 13 control characters (`0x00` through `0x0F`, excluding TAB `0x09`, LF `0x0A`, and CR `0x0D`) that would cause XML parsing failures.
3. **Reformat with XmlReader/XmlWriter** — The cleaned XML is parsed with `XmlReader` (streaming, not DOM) and rewritten with `XmlWriter` as properly indented UTF-8 XML. This validates the structure and produces a clean file.
4. **Save** — The final `.xml` file is saved next to the original `.evtx` with the same name.
5. **Cleanup** — Temporary files are deleted. Garbage collection is forced between files to free memory.
6. **Large file guard** — Files over 500MB are skipped.

### 🔍 Step 3: Keyword Search

After EVTX conversion completes (or is skipped), the main search runs asynchronously on a background thread via `Task.Run()`:

1. **Enumerate files** — `Directory.GetFiles()` recursively lists every file in the directory.
2. **Filter by extension** — Only files ending in `.log`, `.txt`, `.reg`, `.html`, `.json`, or `.xml` are processed.
3. **Read and scan** — For each matching file:
   - All lines are read into memory with `File.ReadAllLines()`.
   - Each line is lowercased and checked with `string.Contains()` against the lowercased keyword.
   - On a match, a `LogResult` object is created with the original sentence, the file path, and the 1-based line number.
4. **Error tolerance** — Per-file errors (out of memory, access denied, unreadable files) are caught and the file is skipped. The search continues with the remaining files.

### 📊 Step 4: Display Results

- If no matches are found, a single placeholder result is shown: *"Nothing was found under that keyword"*.
- Otherwise, all `LogResult` objects are bound to the ListView grid showing Sentence, File Path, and Line Number columns.
- The result count is updated: `"Results Found: N"`.
- Double-clicking a result launches Notepad pointing to that file. Ctrl+C copies the matched sentence to the clipboard.

---

## 💻 Requirements

| Requirement | Details |
|-------------|---------|
| 🖥️ **OS** | Windows (uses `wevtutil` for EVTX conversion, launches `notepad.exe` for file viewing) |
| ⚙️ **Runtime** | .NET Framework 4.7.2 |
| 🔧 **EVTX conversion** | Requires `wevtutil` (included with Windows) |

---

## 📄 License

MIT License. Copyright (c) 2023 IvanRosa. See [LICENSE.txt](LICENSE.txt) for details.
