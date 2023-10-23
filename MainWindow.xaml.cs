using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LogSearchApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public class LogResult
        {
            public string Line { get; set; }
            public string Path { get; set; }
            public int LineNumber { get; set; }
            public string FileName { get; set; }
        }

        private void FolderListBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] folders = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (folders.Length > 0)
                {
                    FolderListBox.Items.Clear();
                    FolderListBox.Items.Add(folders[0]);
                }
            }
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            string logPath = FolderListBox.Items.Count > 0 ? FolderListBox.Items[0] as string : null;
            string searchPattern = SearchTextBox.Text.ToLower();

            if (string.IsNullOrWhiteSpace(logPath) || string.IsNullOrWhiteSpace(searchPattern))
            {
                MessageBox.Show("Please select a log folder and enter a valid search term.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            List<LogResult> results = new List<LogResult>();

            string[] extensions = { ".log", ".txt", ".reg", ".html", ".json", ".xml" };
            string[] files = Directory.GetFiles(logPath, "*.*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                if (Array.Exists(extensions, ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    string[] lines = File.ReadAllLines(file);
                    for (int lineNumber = 0; lineNumber < lines.Length; lineNumber++)
                    {
                        if (lines[lineNumber].ToLower().Contains(searchPattern))
                        {
                            results.Add(new LogResult
                            {
                                Line = lines[lineNumber],
                                Path = file,
                                LineNumber = lineNumber + 1,
                                FileName = Path.GetFileName(file)
                            });
                        }
                    }
                }
            }

            if (results.Count == 0)
            {
                // Display a message when no results are found
                results.Add(new LogResult
                {
                    Line = "Nothing was found under that keyword",
                    Path = "",
                    LineNumber = 0,
                    FileName = ""
                });
            }

            ResultListView.ItemsSource = results;
        }


        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            FolderListBox.Items.Clear();
            SearchTextBox.Clear();
            ResultListView.ItemsSource = null;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
