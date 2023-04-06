using Microsoft.Extensions.Logging;
using System.Diagnostics;

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

            if(!string.IsNullOrWhiteSpace(workingDir))
                psi.WorkingDirectory = workingDir ;

            var proc = new Process
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

            messages.AddRange(msgs);

            return proc.ExitCode;

        }
    }
}
