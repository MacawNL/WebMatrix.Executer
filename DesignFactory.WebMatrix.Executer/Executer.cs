using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WebMatrix.Extensibility;
using Microsoft.WebMatrix.Extensibility.Editor;

namespace DesignFactory.WebMatrix.Executer
{
    public class Executer : DesignFactory.WebMatrix.IExecuter.IExecuter
    {
        private string _tasksource;
        private bool _isCanceled;

        public void Initialize(string tasksource, IWebMatrixHost webMatrixHost, IEditorTaskPanelService editorTaskPanelService)
        {
            _tasksource = tasksource;
            DesignFactory.WebMatrix.Executer.WebMatrixContext.Initialize(webMatrixHost, editorTaskPanelService);
        }

        public void InitializeTabs()
        {
            DesignFactory.WebMatrix.Executer.WebMatrixContext.InitializeTabs();
        }

        public void Write(string format, params object[] args)
        {
            try
            {
                WebMatrixContext.OutputPaneInstance.Write(_tasksource, String.Format(format, args));
            }
            catch
            {
                WebMatrixContext.OutputPaneInstance.Write(_tasksource, format);
            }
        }

        public void WriteLine(string format, params object[] args)
        {
            Write(String.Format(format, args) + Environment.NewLine);
        }

        void ShowOutputPaneAndClearTaskListPane()
        {
            WebMatrixContext.OutputPaneInstance.Clear(_tasksource);
            WebMatrixContext.TaskListPaneInstance.Clear(_tasksource);
            WebMatrixContext.OutputPaneInstance.Show(_tasksource);
        }

        /// <summary>
        /// Run a PowerShell command in the operating system version of PowerShell, so 64 bits PowerShell on 64 bits machine, even when WebMatrix runs in a 32 bits process.
        /// </summary>
        /// <param name="arguments">Arguments to pass to the PowerShell application.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public async Task<bool> RunPowerShellAsync(string arguments)
        {
            var powershellExe = Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe");
            var powershellOSExe = Path.Combine(Path.GetTempPath(), "PowerShellOS.exe");
            if (!File.Exists(powershellOSExe))
            {
                if (!FileUtil.CreateSymbolicLinkToFile(powershellOSExe, powershellExe))
                {
                    throw new FileNotFoundException(
                        String.Format("Could not create symbolic link '{0}' to PowerShell executable", powershellOSExe),
                        powershellExe);
                }
            }
            return await RunAsync(powershellOSExe, arguments);
        }

        public async Task<bool> RunAsync(string fileName, string arguments)
        {
            bool success;

            // specify that the executing task is not canceled
            _isCanceled = false;

            // Make the cancel button active
            WebMatrixContext.OutputPaneInstance.ProcessCancelButton.IsEnabled = true;

            // prep process   
            ProcessStartInfo psi = new ProcessStartInfo(fileName, arguments);
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.CreateNoWindow = true;

            using (Process process = new Process())
            {
                // pass process data     
                process.StartInfo = psi;
                // prep for multithreaded logging     
                ProcessOutputHandler outputHandler = new ProcessOutputHandler(_tasksource, this, process);

                TaskSourceOutput tso = WebMatrixContext.OutputPaneInstance.EnsureTaskSourceOutputForExecutingTask(_tasksource, process, executer:this);
                ShowOutputPaneAndClearTaskListPane();

                WriteLine("Executing: {0} {1}", fileName, arguments);

                await Task.Run(() =>
                {
                    // start process and stream readers, stream readers out-killed when task completes running  
                    process.Start();
                    Task.Run(() => { outputHandler.ReadStdOut(); });
                    Task.Run(() => { outputHandler.ReadStdErr(); });
                    // wait for process to complete     
                    process.WaitForExit();
                });

                // Process is done executing, disconnect process and executer information from output pane
                tso.ExecutingProcess = null;
                tso.ProcessExecuter = null;

                success = process.ExitCode == 0;
            }

            // Make the cancel button inactive, no more process running
            WebMatrixContext.OutputPaneInstance.ProcessCancelButton.IsEnabled = false;

            if (_isCanceled)
            {
                WriteLine(MessageGeneration.Generate(TaskCategory.Warning, "the execution is canceled", String.Empty, String.Empty, 0, 0));
                WriteLine(WebMatrixContext.TaskListPaneInstance.GetTaskListReport(_tasksource, canceled:true));
            }
            else
            {
                WriteLine(WebMatrixContext.TaskListPaneInstance.GetTaskListReport(_tasksource, canceled:false));
                if (WebMatrixContext.TaskListPaneInstance.HasErrors(_tasksource))
                {
                    WebMatrixContext.TaskListPaneInstance.Show(_tasksource);
                }
            }

            return success;
        }

        public bool IsRunning()
        {
            return WebMatrixContext.OutputPaneInstance.HasExecutingTask(_tasksource);
        }

        public void Cancel()
        {
            _isCanceled = true;
            WebMatrixContext.OutputPaneInstance.CancelExecutingTask(_tasksource);
        }
    }
}
