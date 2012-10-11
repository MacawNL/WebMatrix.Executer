using System;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WebMatrix.Extensibility;
using Microsoft.WebMatrix.Extensibility.Editor;
using System.Text.RegularExpressions;

namespace DesignFactory.WebMatrix.Executer
{
    public class Executer : DesignFactory.WebMatrix.IExecuter.IExecuter
    {
        private bool _inSequence = false;   // are we in a sequence of Stat(); ...; result = End(); 
        private string _tasksource;
        private bool _isCanceled;
        private Regex[] _ignoreList = null;
        private Func<string, string> _processLineBeforeParsing = null;

        // Start the sequence
        public void Start(Action cancelAction)
        {
            _inSequence = true;
            ShowOutputPaneAndClearTaskListPane();

            if (cancelAction != null)
            {
                // Make the cancel button active
                WebMatrixContext.OutputPaneInstance.ProcessCancelButton.IsEnabled = true;
            }

            WebMatrixContext.SetCancelAction(cancelAction);
        }

        /// <summary>
        /// We are done with the sequence. 
        /// </summary>
        /// <param name="isCanceled">The sequence is cancelled.</param>
        /// <returns>True if successful (no errors); false otherwise.</returns>
        public bool End(bool isCanceled)
        {
            bool success = true; // assume success!

            // Make the cancel button inactive, no more process running
            WebMatrixContext.OutputPaneInstance.ProcessCancelButton.IsEnabled = false;

            if (isCanceled)
            {
                WriteLine(MessageGeneration.Generate(TaskCategory.Warning, "the execution is canceled", String.Empty, String.Empty, 0, 0));
                WriteLine(WebMatrixContext.TaskListPaneInstance.GetTaskListReport(_tasksource, canceled: true));
                success = false;
            }
            else
            {
                WriteLine(WebMatrixContext.TaskListPaneInstance.GetTaskListReport(_tasksource, canceled: false));
                success = !WebMatrixContext.TaskListPaneInstance.HasErrors(_tasksource);
                if (!success)
                {
                    WebMatrixContext.TaskListPaneInstance.Show(_tasksource);
                }
            }

            _inSequence = false;

            return success;
        }

        public void Write(string format, params object[] args)
        {
            if (!_inSequence)
            {
                throw new ApplicationException("Write() and WriteLine() can only be executed within a sequence starting with Executer.Start(), ending with Executer.End(bool isCanceled).");
            }

            string output;

            // Test if no args are specified. This solves the case where a string is output that contains '{' or '}'
            // characters, for example in processing lines in output from executing process.
            if (args.Length == 0)
            {
                output = format;
            }
            else
            {
                output =  String.Format(format, args);
            }
            WebMatrixContext.OutputPaneInstance.Write(_tasksource, output);

            if (_processLineBeforeParsing != null)
            {
                // transform output
                output = _processLineBeforeParsing(output);
            }
            // Parse the output for warnings and errors
            WebMatrixContext.TaskListPaneInstance.ParseForTask(_tasksource, output, _ignoreList);
        }

        public void WriteLine(string format, params object[] args)
        {
            Write(format, args);
            Write(Environment.NewLine);
        }

        public void ConfigureParsing(Regex[] ignoreList, Func<string, string> processLineBeforeParsing)
        {
            _ignoreList = ignoreList;
            _processLineBeforeParsing = processLineBeforeParsing;
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
            bool miniSequence = false;  // set to true if we are running within our own sequence, no Start() was called, we do it ourselves.

            if (!File.Exists(fileName))
            {
                throw new ApplicationException(String.Format("The application '{0}' does not exist.", fileName));
            }

            if (IsRunning())
            {
                throw new ApplicationException(String.Format("Another task is currently running for '{0}'. Please try again later.", _tasksource));
            }

            if (!_inSequence)
            {
                miniSequence = true;
                Start(() =>
                {
                    WebMatrixContext.OutputPaneInstance.CancelExecutingTask(_tasksource);
                });
            }

            // specify that the executing task is not canceled
            _isCanceled = false;

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

            if (miniSequence)
            {
                success = End(_isCanceled);
            }
        
            return success;
        }

        /// <summary>
        /// Is there a RunAsync() or RunPowerShellAsync() running?
        /// </summary>
        /// <returns></returns>
        public bool IsRunning()
        {
            return WebMatrixContext.OutputPaneInstance.HasExecutingTask(_tasksource);
        }

        /// <summary>
        /// Cancel the currently running RunAsync() or RunPowerShellAsync().
        /// </summary>
        public void Cancel()
        {
            _isCanceled = true;
            WebMatrixContext.OutputPaneInstance.CancelExecutingTask(_tasksource);
        }

        public System.Windows.Threading.Dispatcher UIThreadDispatcher
        {
            get
            {
                // Abuse the output pane to get a dispatcher object for the UI thread
                return WebMatrixContext.OutputPaneInstance.Dispatcher;
            }
        }
        void ShowOutputPaneAndClearTaskListPane()
        {
            WebMatrixContext.OutputPaneInstance.Clear(_tasksource);
            WebMatrixContext.TaskListPaneInstance.Clear(_tasksource);
            WebMatrixContext.OutputPaneInstance.Show(_tasksource);
        }

        public void Initialize(string tasksource, IWebMatrixHost webMatrixHost, IEditorTaskPanelService editorTaskPanelService)
        {
            _tasksource = tasksource;
            DesignFactory.WebMatrix.Executer.WebMatrixContext.Initialize(webMatrixHost, editorTaskPanelService);
        }

        public void InitializeTabs()
        {
            DesignFactory.WebMatrix.Executer.WebMatrixContext.InitializeTabs();

            // Make sure that the tasksource output is available
            TaskSourceOutput tso = WebMatrixContext.OutputPaneInstance.EnsureTaskSourceOutputForExecutingTask(_tasksource, null, executer: this);
        }


    }
}
