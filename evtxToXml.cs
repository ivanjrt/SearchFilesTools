using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace evtxToXml
{
    public static class EvtxToXmlConverter
    {
        private static readonly object _lockObject = new object();
        private static CancellationTokenSource _cancellationTokenSource;

        public static async Task ConvertEvtxFiles(string logPath, Action<double> updateProgress, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(logPath) || !Directory.Exists(logPath))
            {
                updateProgress?.Invoke(0);
                return;
            }

            string[] evtxFiles = new string[0];

            try
            {
                evtxFiles = Directory.GetFiles(logPath, "*.evtx", SearchOption.AllDirectories);
            }
            catch (UnauthorizedAccessException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Access denied when scanning for evtx files: {ex.Message}");
                updateProgress?.Invoke(0);
                return;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error scanning for evtx files: {ex.Message}");
                updateProgress?.Invoke(0);
                return;
            }

            if (evtxFiles.Length == 0)
            {
                updateProgress?.Invoke(100);
                return;
            }

            int totalFiles = evtxFiles.Length;
            int filesProcessed = 0;

            foreach (string evtx in evtxFiles)
            {
                // Check for cancellation
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                string xmlFilePath = Path.Combine(Path.GetDirectoryName(evtx), Path.GetFileNameWithoutExtension(evtx) + ".xml");

                if (!File.Exists(xmlFilePath))
                {
                    await Task.Run(() =>
                    {
                        string tempXmlFile = null;
                        string cleanedXmlFile = null;

                        try
                        {
                            tempXmlFile = RunWevtutilQueryEvents(evtx);

                            if (string.IsNullOrEmpty(tempXmlFile) || !File.Exists(tempXmlFile))
                            {
                                return;
                            }

                            // Check file size to avoid processing enormous files
                            FileInfo fileInfo = new FileInfo(tempXmlFile);
                            if (fileInfo.Length > 500 * 1024 * 1024) // Skip files larger than 500MB
                            {
                                System.Diagnostics.Debug.WriteLine($"Skipping large evtx file: {evtx} ({fileInfo.Length} bytes)");
                                return;
                            }

                            cleanedXmlFile = CleanXmlContentFile(tempXmlFile);

                            // Use XmlReader for streaming to avoid OutOfMemoryException with large files
                            using (XmlReader reader = XmlReader.Create(cleanedXmlFile, new XmlReaderSettings 
                            { 
                                ConformanceLevel = ConformanceLevel.Document,
                                CheckCharacters = false,
                                IgnoreWhitespace = true,
                                IgnoreComments = true
                            }))
                            using (XmlWriter writer = XmlWriter.Create(xmlFilePath, new XmlWriterSettings 
                            { 
                                Indent = true, 
                                Encoding = Encoding.UTF8,
                                ConformanceLevel = ConformanceLevel.Document
                            }))
                            {
                                writer.WriteNode(reader, true);
                            }

                            System.Diagnostics.Debug.WriteLine($"Conversion successful. XML saved to {xmlFilePath}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error converting {evtx}: {ex.Message}");
                            // Continue processing other files even if one fails
                        }
                        finally
                        {
                            // Clean up temporary files
                            if (tempXmlFile != null && File.Exists(tempXmlFile))
                            {
                                try { File.Delete(tempXmlFile); } catch { }
                            }
                            if (cleanedXmlFile != null && File.Exists(cleanedXmlFile))
                            {
                                try { File.Delete(cleanedXmlFile); } catch { }
                            }

                            // Force garbage collection to free memory between iterations
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                        }
                    }, cancellationToken);
                }

                // Update progress bar
                double progress = (double)++filesProcessed / totalFiles * 100;
                updateProgress?.Invoke(progress);
            }

            updateProgress?.Invoke(100);
        }

        private static string RunWevtutilQueryEvents(string evtxFile)
        {
            string tempXmlFile = Path.GetTempFileName();

            try
            {
                using (System.Diagnostics.Process process = new System.Diagnostics.Process())
                {
                    process.StartInfo.FileName = "wevtutil";
                    process.StartInfo.Arguments = $"query-events \"{evtxFile}\" /logfile /element:root";
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                    process.Start();

                    // Stream output to file instead of loading all into memory
                    using (FileStream fileStream = new FileStream(tempXmlFile, FileMode.Create, FileAccess.Write, FileShare.None, 65536))
                    using (StreamWriter writer = new StreamWriter(fileStream, Encoding.UTF8, 65536))
                    using (StreamReader reader = process.StandardOutput)
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            writer.WriteLine(line);
                        }
                    }

                    int exitCode = process.ExitCode;

                    if (exitCode != 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"wevtutil failed with exit code {exitCode} for file: {evtxFile}");
                        if (File.Exists(tempXmlFile))
                        {
                            try { File.Delete(tempXmlFile); } catch { }
                        }
                        return null;
                    }
                }

                return tempXmlFile;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error running wevtutil: {ex.Message}");
                if (File.Exists(tempXmlFile))
                {
                    try { File.Delete(tempXmlFile); } catch { }
                }
                return null;
            }
        }

        private static string CleanXmlContentFile(string xmlFilePath)
        {
            string cleanedXmlFile = Path.GetTempFileName();

            try
            {
                // Stream through the file to clean it, avoiding loading entire content into memory
                using (StreamReader reader = new StreamReader(xmlFilePath, Encoding.UTF8, true, 65536))
                using (StreamWriter writer = new StreamWriter(cleanedXmlFile, false, Encoding.UTF8, 65536))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        // Remove problematic characters that can cause XML parsing errors
                        line = line.Replace("\x00", "")  // NULL
                                   .Replace("\x01", "")  // SOH
                                   .Replace("\x02", "")  // STX
                                   .Replace("\x03", "")  // ETX
                                   .Replace("\x04", "")  // EOT
                                   .Replace("\x05", "")  // ENQ
                                   .Replace("\x06", "")  // ACK
                                   .Replace("\x07", "")  // BEL
                                   .Replace("\x08", "")  // BS
                                   .Replace("\x0B", "")  // VT
                                   .Replace("\x0C", "")  // FF
                                   .Replace("\x0E", "")  // SO
                                   .Replace("\x0F", ""); // SI
                        writer.WriteLine(line);
                    }
                }

                return cleanedXmlFile;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cleaning XML content: {ex.Message}");
                if (File.Exists(cleanedXmlFile))
                {
                    try { File.Delete(cleanedXmlFile); } catch { }
                }
                throw;
            }
        }
    }
}
