using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace DesignFactory.WebMatrix.Executer
{
    /// <summary>
    /// Interaction logic for OutputWindow.xaml
    /// </summary>
    internal partial class OutputPane : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged; 

        public static readonly Guid OutputPaneTaskPanelId = new Guid("921D0083-CFFC-495F-9B35-912B52476697");
        private ObservableCollection<TaskSourceOutput> _taskSourceOutputs = new ObservableCollection<TaskSourceOutput>();
        private TaskSourceOutput _currentTaskSourceOutput = null;

        public OutputPane()
        {
            InitializeComponent();
        }

        public Action CancelAction { get; set; }

        /// <summary>
        /// Required for binding.
        /// </summary>
        public ObservableCollection<TaskSourceOutput> TaskSourceOutputs
        {
            get { return _taskSourceOutputs; }
        }

        public TaskSourceOutput CurrentTaskSourceOutput
        {
            get
            {
                return _currentTaskSourceOutput;
            }

            set
            {
                if (_currentTaskSourceOutput != value)
                {
                    // Moving over to a new task source output, switch text and notify
                    _currentTaskSourceOutput = value;
                    OutputTextBox.Text = _currentTaskSourceOutput.Output;
                    PropertyChanged(this, new PropertyChangedEventArgs("CurrentTaskSourceOutput"));
                }
            }
        }

        TaskSourceOutput GetTaskSourceOutput(string tasksource)
        {
            return _taskSourceOutputs.Where(n => n.TaskSource == tasksource).FirstOrDefault();
        }

        /// <summary>
        /// Ensure that a task source for the output pane is created. If it is the first task source output, make it the visible task source output.
        /// </summary>
        /// <param name="tasksource">Name of the task source.</param>
        /// <param name="executingTask">The currently executing task.</param>
        /// <param name="executingTask">The executer the currently executing task is running in.</param>
        /// <returns>The existing or newly created task source output.</returns>
        internal TaskSourceOutput EnsureTaskSourceOutputForExecutingTask(string tasksource, Process executingProcess, Executer executer)
        {
            bool makeVisible = false;
            TaskSourceOutput tso;

            if (CurrentTaskSourceOutput == null)
            {
                makeVisible = true;
            }

            // Get TaskSource output
            tso = GetTaskSourceOutput(tasksource);
            if (tso == null)
            {
                tso = new TaskSourceOutput { TaskSource = tasksource, ExecutingProcess = executingProcess, ProcessExecuter = executer, Output = String.Empty };
                _taskSourceOutputs.Add(tso);
            }
            else
            {
                tso.ExecutingProcess = executingProcess;
                tso.ProcessExecuter = executer;
            }

            if (makeVisible)
            {
                CurrentTaskSourceOutput = tso;
                WebMatrixContext.OutputPaneInstance.Show(tasksource);
            }

            return tso;
        }

        public void Clear(string tasksource)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                TaskSourceOutput tso = GetTaskSourceOutput(tasksource);

                // TaskSourceOutput must exist before we write to it.
                Debug.Assert(tso != null);
                
                if (tso != null) // be defensive
                {
                    tso.Output = String.Empty;
                    if (tso == CurrentTaskSourceOutput)
                    {
                        OutputTextBox.Text = String.Empty;
                        ScrollViewer.ScrollToEnd();
                    }
                }
            }));
        }

        /// <summary>
        /// Write a text to the given task source output.
        /// </summary>
        /// <param name="tasksource">Name of the task source to output to.</param>
        /// <param name="text">The text to write to output.</param>
        public void Write(string tasksource, string text)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                TaskSourceOutput tso = GetTaskSourceOutput(tasksource);

                // TaskSourceOutput must exist before we write to it.
                Debug.Assert(tso != null);

                if (tso != null) // be defensive
                {
                    tso.Output += text;
                    if (tso == CurrentTaskSourceOutput)
                    {
                        OutputTextBox.Text += text;
                        ScrollViewer.ScrollToEnd();
                    }
                }
            }));
        }

        public void Show(string tasksource)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                WebMatrixContext.EditorTaskPanelService.ShowBottomPane();
                WebMatrixContext.EditorTaskPanelService.ShowTaskTab(OutputPane.OutputPaneTaskPanelId);
                CurrentTaskSourceOutput = _taskSourceOutputs.Where(n => n.TaskSource == tasksource).FirstOrDefault();
            }));

        }

        public bool HasExecutingTask(string tasksource)
        {
            TaskSourceOutput tso = GetTaskSourceOutput(tasksource);
            return (tso != null && tso.ExecutingProcess != null);
        }

        public void CancelExecutingTask(string tasksource)
        {
            TaskSourceOutput tso = GetTaskSourceOutput(tasksource);
            if (tso != null && tso.ExecutingProcess != null)
            {
                tso.ExecutingProcess.Kill();
            }
        }

        private void OutputPaneClearButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (CurrentTaskSourceOutput != null)
            {
                Clear(CurrentTaskSourceOutput.TaskSource);
            }
        }

        private void WordWrapCheckBox_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (WordWrapCheckBox.IsChecked == true)
            {
                OutputTextBox.TextWrapping = System.Windows.TextWrapping.WrapWithOverflow;
            }
            if (WordWrapCheckBox.IsChecked == false)
            {
                OutputTextBox.TextWrapping = System.Windows.TextWrapping.NoWrap;
            }
        }

        private void ProcessCancelButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (CancelAction != null)
            {
                CancelAction();
            }
        }
    }
}
