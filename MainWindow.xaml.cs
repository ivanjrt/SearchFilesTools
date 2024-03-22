using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using evtxToXml;
using System.IO.Compression;


namespace LogSearchApp
{
    public partial class MainWindow : Window
    {
        public class LogResult
        {
            public string Sentence { get; set; }
            public string Path { get; set; }
            public int LineNumber { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();

            // Attach the PreviewKeyDown event to ResultListView
            ResultListView.PreviewKeyDown += ResultListView_PreviewKeyDown;
        }

        private void ResultListView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Check if Ctrl + C is pressed
            if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // Copy the selected item's "Sentence" to the clipboard
                CopySelectedItemToClipboard();
            }
        }

        private void CopySelectedItemToClipboard()
        {
            if (ResultListView.SelectedItems.Count > 0)
            {
                var selectedItem = ResultListView.SelectedItems[0] as LogResult;
                if (selectedItem != null)
                {
                    Clipboard.SetText(selectedItem.Sentence);
                }
            }
        }
        private async void FolderListBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] droppedItems = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (droppedItems.Length > 0)
                {
                    string path = droppedItems[0];

                    // Check if the dropped item is a zip file
                    if (Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        // Display message and activate progress bar during uncompressing
                        ResultCountTextBlock.Text = "Uncompressing...";
                        SearchProgressBar.Visibility = Visibility.Visible;

                        try
                        {
                            // Extract the contents of the zip file to a folder with the same path
                            string extractPath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
                            await Task.Run(() => ZipFile.ExtractToDirectory(path, extractPath));

                            // Update the log path to the extracted folder
                            path = extractPath;

                            // Hide progress bar/results after uncompressing
                            SearchProgressBar.Visibility = Visibility.Collapsed;
                            ResultCountTextBlock.Text = "File Uncompressed and Ready to Search";
                        }
                        catch (Exception ex)
                        {
                            // Handle the exception (display or log the error)
                            MessageBox.Show($"Error during extraction: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            MessageBox.Show("Please Unzip the file Manually then re-open the program and add the folder to search Error:001", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                            // Exit the application
                            Environment.Exit(1);
                        }
                    }

                    FolderListBox.Items.Clear();
                    FolderListBox.Items.Add(path);
                }
            }
        }



        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            string logPath = FolderListBox.Items.Count > 0 ? FolderListBox.Items[0] as string : null;
            string searchPattern = SearchTextBox.Text.ToLower();

            if (string.IsNullOrWhiteSpace(logPath) || string.IsNullOrWhiteSpace(searchPattern))
            {
                MessageBox.Show("Please select a log folder and enter a valid search term.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Show the progress bar
            ResultCountTextBlock.Text = "Searching...";
            SearchProgressBar.Visibility = Visibility.Visible;

            // Check for .evtx files and convert them to XML
            await EvtxToXmlConverter.ConvertEvtxFiles(logPath, progress =>
            {
                // Update the progress bar for evtx to XML conversion
                Dispatcher.Invoke(() =>
                {
                    SearchProgressBar.Value = progress;
                });
            });



            // Perform the search in the background
            List<LogResult> results = await Task.Run(() =>
            {
                string[] extensions = { ".log", ".txt", ".reg", ".html", ".json", ".xml" };
                string[] files = Directory.GetFiles(logPath, "*.*", SearchOption.AllDirectories);

                List<LogResult> searchResults = new List<LogResult>();

                foreach (var file in files)
                {
                    if (Array.Exists(extensions, ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                    {
                        string[] lines = File.ReadAllLines(file);
                        for (int lineNumber = 0; lineNumber < lines.Length; lineNumber++)
                        {
                            if (lines[lineNumber].ToLower().Contains(searchPattern))
                            {
                                searchResults.Add(new LogResult
                                {
                                    Sentence = lines[lineNumber],
                                    Path = file,
                                    LineNumber = lineNumber + 1,
                                });
                            }
                        }
                    }
                }

                return searchResults;
            });

            // Hide the progress bar after the search is completed
            SearchProgressBar.Visibility = Visibility.Collapsed;

            if (results.Count == 0)
            {
                // Display a message when no results are found
                results.Add(new LogResult
                {
                    Sentence = "Nothing was found under that keyword",
                    Path = "",
                    LineNumber = 0,
                });
            }

            ResultListView.ItemsSource = results;

            // Update the result count TextBlock
            ResultCountTextBlock.Text = $"Results Found: {results.Count}";

            // Attach a click event to open files in Notepad
            ResultListView.MouseDoubleClick += ResultListView_MouseDoubleClick;
        }

        private void ResultListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedItem = ResultListView.SelectedItem as LogResult;
            if (selectedItem != null)
            {
                if (File.Exists(selectedItem.Path))
                {
                    try
                    {
                        Process.Start("notepad.exe", selectedItem.Path);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error opening file with Notepad: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        MessageBox.Show($"Error code 002", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            FolderListBox.Items.Clear();
            SearchTextBox.Clear();
            ResultListView.ItemsSource = null;
            ResultCountTextBlock.Text = "Results Found: 0";
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Build 8" +
                "\n\nThis App will search keywords within log, txt, reg, html, json, and .xml files\nThis App will convert evtx files to xml for better handling" +
                "\n\nThe author assumes no responsibility or liability for any errors using this App", "About", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void FolderListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("If issues extracting a Zip file: ensure that the extracted content does not reside in the same location with an identical name as the " +
                "original Zip file, otherwise try to unzip it manually","Help", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void SearchTextBox_Keydown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Search_Click(sender, e);
            }
        }
    }
}