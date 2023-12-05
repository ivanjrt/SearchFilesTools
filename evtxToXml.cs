using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace evtxToXml
{
    public static class EvtxToXmlConverter
    {
        public static async Task ConvertEvtxFiles(string logPath, Action<double> updateProgress)
        {
            string[] evtxFiles = Directory.GetFiles(logPath, "*.evtx", SearchOption.AllDirectories);
            int totalFiles = evtxFiles.Length;
            int filesProcessed = 0;

            foreach (string evtx in evtxFiles)
            {
                string xmlFilePath = Path.Combine(Path.GetDirectoryName(evtx), Path.GetFileNameWithoutExtension(evtx) + ".xml");

                if (!File.Exists(xmlFilePath))
                {
                    await Task.Run(() =>
                    {
                        string evtxContent = RunWevtutilQueryEvents(evtx);
                        string cleanedXmlContent = CleanXmlContent(evtxContent);

                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(cleanedXmlContent);

                        xmlDoc.Save(xmlFilePath);
                        Console.WriteLine($"Conversion successful. XML saved to {xmlFilePath}");

                        // Update progress bar
                        double progress = (double)++filesProcessed / totalFiles * 100;
                        updateProgress?.Invoke(progress);
                    });
                }
            }
        }

        private static string RunWevtutilQueryEvents(string evtxFile)
        {
            using (System.Diagnostics.Process process = new System.Diagnostics.Process())
            {
                process.StartInfo.FileName = "wevtutil";
                process.StartInfo.Arguments = $"query-events \"{evtxFile}\" /logfile /element:root";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return output;
            }
        }

        private static string CleanXmlContent(string xmlContent)
        {
            return xmlContent.Replace("\x01", "").Replace("\x0f", "").Replace("\x02", "");
        }
    }
}
