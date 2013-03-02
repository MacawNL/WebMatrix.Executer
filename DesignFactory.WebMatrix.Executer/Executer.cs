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
        private bool _inSequence = false;   // are we in a sequence of Start(); ...; result = End(); 
        private string _tasksource;
        private IWebMatrixHost _webMatrixHost;
        private bool _isCanceled;
        private Regex[] _ignoreList = null;
        private Func<string, string> _processLineBeforeParsing = null;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public void ReinitializeCancellationTokenSource()
        {
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public CancellationTokenSource GetCancellationTokenSource()
        {
            return _cancellationTokenSource;
        }

        public CancellationToken GetCancellationToken()
        {
            return _cancellationTokenSource.Token;
        }

        // Start the sequence
        public bool Start(Action cancelAction = null)
        {
            InitializeTabs();
            ShowOutputPaneAndClearTaskListPane();
            if (IsRunning())
            {
                this.UIThreadDispatcher.Invoke(new Action(() =>
                {
                     WriteLineNoParse("There is already a '{0}' action running.", _tasksource);
                }));
                return false; // can't start
            }

            _inSequence = true;
            _isCanceled = false;

            ReinitializeCancellationTokenSource();

            // Make the cancel button active
            this.UIThreadDispatcher.Invoke(new Action(() =>
            {
                WebMatrixContext.OutputPaneInstance.ProcessCancelButton.IsEnabled = true;
            }));

            WebMatrixContext.SetCancelAction(new Action(() =>
                {
                    _cancellationTokenSource.Cancel();  // cancel any async tasks started with cancellation token
                    if (cancelAction != null)           // execute the user defined cancellation action
                    {
                        cancelAction();
                    }
                    _isCanceled = true;
                }));

            return true; // can start!
        }

        /// <summary>
        /// We are done with the sequence. 
        /// </summary>
        /// <returns>True if successful (no errors); false otherwise.</returns>
        public bool End()
        {
            string taskListReport = WebMatrixContext.TaskListPaneInstance.GetTaskListReport(_tasksource, canceled: _isCanceled);
            bool success = !WebMatrixContext.TaskListPaneInstance.HasErrors(_tasksource);
            
            this.UIThreadDispatcher.Invoke(new Action(() =>
                {
                    WriteLineNoParse(taskListReport);
                    // Make the cancel button inactive, no more process running
                    WebMatrixContext.OutputPaneInstance.ProcessCancelButton.IsEnabled = false;
                    if (!success && !_isCanceled)
                    {
                        this.UIThreadDispatcher.Invoke(new Action(() => WebMatrixContext.TaskListPaneInstance.Show(_tasksource)));
                    }

                    // refresh the tree
                    var refresh = _webMatrixHost.HostCommands.GetCommand(Microsoft.WebMatrix.Extensibility.CommonCommandIds.GroupId, (int)Microsoft.WebMatrix.Extensibility.CommonCommandIds.Ids.Refresh);
                    if (refresh.CanExecute(null))
                    {
                        refresh.Execute(null);
                    }
                }));

            _inSequence = false;

            return success;
        }

        public void Write(string format, params object[] args)
        {
            //if (!_inSequence)
            //{
            //    throw new ApplicationException("Write() and WriteLine() can only be executed within a sequence starting with Executer.Start(), ending with Executer.End(bool isCanceled).");
            //}

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

            this.UIThreadDispatcher.Invoke(new Action(() =>
                {
                    WebMatrixContext.OutputPaneInstance.Write(_tasksource, output);
                }));

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

        public void WriteNoParse(string format, params object[] args)
        {
            //if (!_inSequence)
            //{
            //    throw new ApplicationException("WriteNoParse() and WriteLineNoParse() can only be executed within a sequence starting with Executer.Start(), ending with Executer.End().");
            //}

            string output;

            // Test if no args are specified. This solves the case where a string is output that contains '{' or '}'
            // characters, for example in processing lines in output from executing process.
            if (args.Length == 0)
            {
                output = format;
            }
            else
            {
                output = String.Format(format, args);
            }
            WebMatrixContext.OutputPaneInstance.Write(_tasksource, output);
        }

        public void WriteLineNoParse(string format, params object[] args)
        {
            WriteNoParse(format, args);
            WriteNoParse(Environment.NewLine);
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
        public Task<bool> RunPowerShellAsync(string arguments)
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
            return RunAsync(powershellOSExe, arguments);
        }

        public Task<bool> RunAsync(string fileName, string arguments)
        {
            bool miniSequence = false;  // set to true if we are running within our own sequence, no Start() was called, we do it ourselves.

            if (!File.Exists(fileName))
            {
                throw new ApplicationException(String.Format("The application '{0}' does not exist.", fileName));
            }

            if (IsRunning())
            {
                throw new ApplicationException(String.Format("Another task is currently running for '{0}'. Please try again later.", _tasksource));
            }

            // specify that the executing task is not canceled
            //_isCanceled = false;

            WriteLine("Executing: {0} {1}", fileName, arguments);

            Task<bool> mainTask = Task.Factory.StartNew(() =>
                {
                    bool success;

                    if (!_inSequence)
                    {
                        miniSequence = true;
                        Start(() =>
                        {
                            WebMatrixContext.OutputPaneInstance.CancelExecutingTask(_tasksource);
                        });
                    }

                    // prep process   
                    ProcessStartInfo psi = new ProcessStartInfo(fileName, arguments);
                    psi.UseShellExecute = false;
                    psi.RedirectStandardOutput = true;
                    psi.RedirectStandardError = true;
                    psi.CreateNoWindow = true;

                    using (var process = new Process())
                    {
                        // pass process data     
                        process.StartInfo = psi;
                        // prep for multithreaded logging     
                        var outputHandler = new ProcessOutputHandler(_tasksource, this, process);

                        TaskSourceOutput tso = WebMatrixContext.OutputPaneInstance.EnsureTaskSourceOutputForExecutingTask(_tasksource, process, executer: this);
                        // start process and stream readers, stream readers out-killed when task completes running  
                        process.Start();
                        Task stderrTask = Task.Factory.StartNew(() => { outputHandler.ReadStdOut(); });
                        Task stdoutTask = Task.Factory.StartNew(() => { outputHandler.ReadStdErr(); });
                        // wait for process to complete     
                        process.WaitForExit();
                        // Process is done executing, disconnect process and executer information from output pane
                        tso.ExecutingProcess = null;
                        tso.ProcessExecuter = null;

                        success = process.ExitCode == 0;
                    }

                    if (miniSequence)
                    {
                        success = End();
                    }

                    return success;
                }, GetCancellationToken());

            return mainTask;
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
            _webMatrixHost = webMatrixHost;
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
