# OutOfMemoryException Fix - Root Cause & Solutions

## Root Cause Analysis

The `System.OutOfMemoryException` was occurring during application startup due to:

1. **Large Event Log Files**: The application processes `.evtx` (Windows Event Log) files by converting them to XML using the `wevtutil` command-line tool.
2. **Unbounded Memory Allocation**: When `wevtutil` outputs event log data, especially for large logs, intermediate XML files could exhaust available memory.
3. **XML Parsing Errors**: Malformed XML characters in event logs could cause excessive memory allocation during XML validation and cleanup.
4. **Synchronous Processing**: The conversion was happening immediately during the search operation, blocking the UI thread and preventing graceful error handling.

## Changes Made

### 1. **evtxToXml.cs** - Enhanced Error Handling & Safety

#### Key Improvements:
- **File size checks**: Skip event logs larger than 500MB to prevent memory exhaustion
- **Process error detection**: Detect `wevtutil` failures and return null gracefully
- **Expanded character filtering**: Removed all control characters (0x00-0x0F) instead of just three specific ones
- **Try-catch blocks**: Wrapped directory scanning and file operations with proper exception handling
- **Cancellation token support**: Added ability to cancel ongoing conversions
- **XmlReaderSettings optimization**: 
  - Added `CheckCharacters = false` to skip strict XML validation
  - Added `IgnoreWhitespace = true` and `IgnoreComments = true` to reduce memory footprint
- **Return value handling**: Methods now return `null` on failure instead of throwing unhandled exceptions

### 2. **MainWindow.xaml.cs** - Robust Search Implementation

#### Key Improvements:
- **Exception handling for ConvertEvtxFiles()**:
  - Catches `OutOfMemoryException` and shows user-friendly message
  - Continues with regular file search if evtx conversion fails
  - Logs errors to debug output

- **Per-file exception handling during search**:
  - Skips files too large to process in memory
  - Continues processing remaining files even if one fails
  - Prevents one corrupted file from breaking the entire search

- **Directory access handling**:
  - Catches `UnauthorizedAccessException` for protected folders
  - Logs inaccessible paths for debugging

- **Zip extraction improvements**:
  - Skips extraction if directory already exists
  - Catches `OutOfMemoryException` during zip expansion
  - Shows specific message for memory-limited extraction

## Why These Changes Fix the Issue

1. **Prevents Out-of-Memory During Startup**: The application no longer automatically processes all event logs on startup. Conversion only happens when user clicks "Search".

2. **Graceful Degradation**: If event log conversion fails, the app continues with regular file search instead of crashing.

3. **Memory-Aware Processing**: Large files are skipped with appropriate logging rather than attempting to load them entirely into memory.

4. **Better XML Handling**: Expanded character filtering prevents XML parser errors from consuming excessive memory.

5. **User Feedback**: Clear error messages help users understand what went wrong and how to fix it (removing large log files, etc.).

## Testing Recommendations

1. **Test with large event logs**: Create a test folder with multi-GB .evtx files
2. **Test with corrupted XML**: Manually create malformed .evtx files
3. **Test with protected folders**: Try searching in System32 or other restricted directories
4. **Test with large zip files**: Verify graceful handling of large archives
5. **Monitor memory usage**: Use Task Manager to verify memory doesn't spike unexpectedly

## Performance Impact

- **Startup time**: Slightly faster (no background processing)
- **Search time**: Negligible difference; same core search algorithm
- **Memory usage**: Lower peak memory; scales better with large directories
- **User experience**: Better responsiveness and error messaging
