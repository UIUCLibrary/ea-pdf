﻿using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace UIUCLibrary.EaPdf.Helpers
{
    public class JavaRunner
    {
        const string DEFAULT_JAVA_EXEC = "java"; //default assumes that java is in the path
        const string DEFAULT_MAX_MEMORY = "1024m";

        public string JavaExec { get; set; } = DEFAULT_JAVA_EXEC;
        public string MaxMemory { get; set; } = DEFAULT_MAX_MEMORY;
        public string ClassPath { get; set; } = "";

        public JavaRunner(string javaExec, string maxMem, string classPath)
        {
            JavaExec = javaExec;
            MaxMemory = maxMem;
            ClassPath = classPath;
        }
        public JavaRunner(string maxMem, string classPath) : this(DEFAULT_JAVA_EXEC, maxMem, classPath)
        {
        }
        public JavaRunner(string classPath) : this(DEFAULT_MAX_MEMORY, classPath)
        {
        }

        public JavaRunner() : this("")
        {
        }

        public int RunExecutableJar(string jarPath, string arguments, ref List<(LogLevel level, string message)> messages)
        {
            var args = "";

            if (!string.IsNullOrWhiteSpace(MaxMemory))
                args += $" -Xmx{MaxMemory}";

            if (!string.IsNullOrWhiteSpace(ClassPath))
                args += $" -cp \"{ClassPath}\"";

            if (!string.IsNullOrWhiteSpace(jarPath))
                args += $" -jar \"{jarPath}\"";
            else
                throw new ArgumentNullException(nameof(jarPath));

            if (!string.IsNullOrWhiteSpace(arguments))
                args += $" {arguments}";

            var workingDir = Path.GetDirectoryName(jarPath) ?? "";  //for fop the working dir should contain the fop.jar file

            return Run(args, workingDir, ref messages);
        }

        public int RunMainClass(string mainClass, ref List<(LogLevel level, string message)> messages)
        {
            return this.RunMainClass(mainClass, "", ref messages);
        }


        public int RunMainClass(string mainClass, string arguments, ref List<(LogLevel level, string message)> messages)
        {
            var args = "";

            if (!string.IsNullOrWhiteSpace(MaxMemory))
                args += $" -Xmx{MaxMemory}";

            if (!string.IsNullOrWhiteSpace(ClassPath))
                args += $" -cp \"{ClassPath}\"";

            if (!string.IsNullOrWhiteSpace(mainClass))
                args += $" {mainClass}";
            else
                throw new ArgumentNullException(nameof(mainClass));

            if (!string.IsNullOrWhiteSpace(arguments))
                args += $" {arguments}";


            return Run(args, ref messages);
        }

        public int Run(string arguments, ref List<(LogLevel level, string message)> messages)
        {
            return Run(arguments, "", ref messages);
        }

        public int Run(string arguments, string workingDir, ref List<(LogLevel level, string message)> messages)
        {
            var psi = new ProcessStartInfo
            {
                FileName = JavaExec,
                Arguments = arguments,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };

            if (!string.IsNullOrWhiteSpace(workingDir))
                psi.WorkingDirectory = workingDir;

            messages.Add((LogLevel.Trace, $"Running: {psi.FileName} {psi.Arguments}"));
            if (!string.IsNullOrWhiteSpace(psi.WorkingDirectory))
            {
                messages.Add((LogLevel.Trace, $"Working Directory: {psi.WorkingDirectory}"));
            }
            messages.Add((LogLevel.Trace, $"Current Directory: {Directory.GetCurrentDirectory()}"));

            using var proc = new Process
            {
                StartInfo = psi
            };
            proc.Start();

            var msgs = new List<(LogLevel level, string message)>();

            proc.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data)) msgs.Add((LogLevel.Information, e.Data));
            };

            proc.BeginOutputReadLine();

            proc.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data)) msgs.Add((LogLevel.Warning, e.Data));
            };

            proc.BeginErrorReadLine();

            proc.WaitForExit();

            messages.InsertRange(0, msgs); //insert the messages at the beginning of the list

            return proc.ExitCode;

        }

        //used by child subclasses to convert message lines to the correct log level and granularity
        protected static void AppendMessage(ref LogLevel logLevel, ref StringBuilder messageAccumulator, ref List<(LogLevel level, string message)> messages)
        {
            if (logLevel != LogLevel.None)
            {
                messages.Add((logLevel, messageAccumulator.ToString().Trim()));
                messageAccumulator.Clear();
                logLevel = LogLevel.None;
            }
            else
            {
                throw new Exception("Unable to determine log level");
            }
        }

    }
}
