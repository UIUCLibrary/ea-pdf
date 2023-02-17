using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using PdfTemplating.XslFO.ApacheFOP.Serverless;

namespace UIUCLibrary.EaPdf
{
    public class XmlToPdfProcessor
    {
        public const string FO_XSLT = "eaxs_to_fo.xslt";

        public static void Process(string xmlFilePath, string xslFilePath, string pdfFilePath)
        {
            var foFilePath = Path.ChangeExtension(xmlFilePath, ".fo");

            var psi = new ProcessStartInfo();

            psi.FileName = "java";
            psi.Arguments = $@"-cp ""C:\Program Files\SaxonHE11-5J\saxon-he-11.5.jar"" net.sf.saxon.Transform -s:""{xmlFilePath}"" -xsl:""{xslFilePath}"" -o:""{foFilePath}"" fo-processor=fop";
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;

            var proc = new Process();
            proc.StartInfo = psi;
            proc.Start();

            List<string> outLines= new();
            List<string> errLines= new();
            while (!proc.StandardOutput.EndOfStream)
            {
                outLines.Add(proc.StandardOutput.ReadLine() ?? "");
            }
            while (!proc.StandardError.EndOfStream)
            {
                errLines.Add(proc.StandardError.ReadLine() ?? "");
            }

            proc.WaitForExit();
        }
    }
}
