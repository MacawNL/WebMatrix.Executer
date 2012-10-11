using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.WebMatrix.Extensibility;
using Microsoft.WebMatrix.Extensibility.Editor;

namespace DesignFactory.WebMatrix.Executer
{
    /// <summary>
    /// Interaction logic for TaskList.xaml
    /// </summary>
    internal partial class TaskListPane : UserControl, INotifyPropertyChanged
    {
        public static readonly Guid TaskListPaneTaskPanelId = new Guid("C2EEEBDA-FDF0-4946-B095-15A8B1030886");
        public static readonly string _allTaskSourcesString = "[all]";

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Observable collection with all tasks in the task list.
        /// </summary>
        private ObservableCollection<TaskItem> _tasks = new ObservableCollection<TaskItem>();
        
        /// <summary>
        /// Collection with all task sources, and as first element _allTaskSourcesString ("[all]").
        /// </summary>
        private ObservableCollection<string> _tasksources = new ObservableCollection<string>();

        /// <summary>
        /// The currently selected task source.
        /// </summary>
        private string _currentTaskSource;


        public TaskListPane()
        {
            InitializeComponent();

            // Clear the tasks list (_tasks), makes sure the task sources list (_taskources) contains the "all task sources selector"
            Clear();

            // Enable filtering based on UI selections for the grid view
            CollectionViewSource vs = (CollectionViewSource)this.Resources["FilteredTasks"];
            vs.Filter += FilterTasks;
        }

        /// <summary>
        /// Required for binding.
        /// </summary>
        public ObservableCollection<TaskItem> Tasks
        {
            get { return _tasks; }
        }

        /// <summary>
        /// Required for binding.
        /// </summary>
        public ObservableCollection<string> TaskSources
        {
            get { return _tasksources; }
        }

        public string CurrentTaskSource
        {
            get
            {
                return _currentTaskSource;
            }

            set
            {
                if (_currentTaskSource != value)
                {
                    _currentTaskSource = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("CurrentTaskSource"));
                }
            }
        }

        /// <summary>
        /// Filter function connected to the CollectionViewSource of "FilteredTasks".
        /// Show errors and warnings based on task source selection and show warnings/errors selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FilterTasks(object sender, FilterEventArgs e)
        {
            var taskitem = e.Item as TaskItem;
            if (taskitem == null)
            {
                e.Accepted = false;
            }
            else if ((CurrentTaskSource != _allTaskSourcesString && taskitem.TaskSource != CurrentTaskSource) ||
                (ShowErrorsCheckBox.IsChecked == false && taskitem.Category == TaskCategory.Error) ||
                (ShowWarningsCheckBox.IsChecked == false && taskitem.Category == TaskCategory.Warning))
            {
                e.Accepted = false;
            }
        }

        /// <summary>
        /// If filename is relative, make path absolute if it was relative to workspace and exists.
        /// </summary>
        /// <param name="filename">The possiblt relative filename.</param>
        /// <returns>The absolute path, if filename relative to workspace, otherwise original path.</returns>
        public string MakeWorkspaceRelativeFilenameAbsoluteIfPossible(string filename)
        {
            if (!Path.IsPathRooted(filename))
            {
                string localWebSitePath = WebMatrixContext.WebMatrixHost.WebSite != null ? WebMatrixContext.WebMatrixHost.WebSite.Path : "<NOPATH>";
                if (localWebSitePath != "<NOPATH>")
                {
                    string absoluteFilename = Path.Combine(localWebSitePath, filename);
                    if (File.Exists(absoluteFilename))
                    {
                        filename = absoluteFilename;
                    }
                }
            }
            return filename;
        }

        /// <summary>
        /// Return the first uri in the text.
        /// </summary>
       /// <param name="text">Text to parse.</param>
        /// <returns>The uri, or null if not found.</returns>
        private string GetHelpLink(string text)
        {
            string uri = null;
            Match uriMatches = Regex.Match(text,
                       @"((https?|ftp|gopher|telnet|file|notes|ms-help):((//)|(\\\\))+[\w\d:#@%/;$()~_?\+-=\\\.&]*)");
            if (uriMatches.Success)
            {
                uri = uriMatches.Captures[0].Value;
            }

            return uri;
        }

        /// <summary>
        /// Clear the task list and the task sources list, make the "all task sources" filter the current filter.
        /// </summary>
        public void Clear()
        {
            _tasks.Clear();
            _tasksources.Clear();
            _tasksources.Add(_allTaskSourcesString);
            CurrentTaskSource = _allTaskSourcesString;
        }

        /// <summary>
        /// Remove the task list from warnings and errors for the given task source.
        /// The task source is NOT removed from the task sources list.
        /// </summary>
        /// <param name="tasksource">The task source to remove warnings and errors for.</param>
        public void Clear(string tasksource)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                if (_tasks.Count > 0)
                {
                    var toRemove = _tasks.Where(n => n.TaskSource == tasksource).ToList();
                    if (toRemove != null)
                    {
                        foreach (var item in toRemove)
                        {
                            _tasks.Remove(item);
                        }
                    }
                }
            }));
        }

        /// <summary>
        /// Add a task to the task list.
        /// </summary>
        /// <param name="tasksource"></param>
        /// <param name="category"></param>
        /// <param name="text"></param>
        /// <param name="code"></param>
        /// <param name="filename"></param>
        /// <param name="linenumber"></param>
        /// <param name="column"></param>
        public void AddTask(string tasksource, TaskCategory category, string text, string code, string filename, int linenumber, int column)
        {
            this.Dispatcher.Invoke(new Action(() =>
             {
                 string helplink = GetHelpLink(text);
                 string absoluteFilename = MakeWorkspaceRelativeFilenameAbsoluteIfPossible(filename);
                 _tasks.Add(new TaskItem
                 {
                     TaskSource = tasksource,
                     Category = category,
                     Text = text,
                     HasHelpLink = helplink != null,
                     HelpLink = helplink,
                     Code = code,
                     WorkspaceRelativeFilename = WebMatrixContext.GetWorkspaceRelativeFilename(absoluteFilename),
                     Filename = absoluteFilename,
                     Linenumber = linenumber,
                     Column = column
                 });

                 // If this task source is not in the list of task sources, add it
                 if (_tasksources.Where(n => n == tasksource).FirstOrDefault() == null)
                 {
                     _tasksources.Add(tasksource);
                 }

             }));
        }

        /// <summary>
        /// Parse an output line for error or warning.
        /// </summary>
        /// <param name="tasksource">Source (application) where error is comming from.</param>
        /// <param name="line">
        /// Output line to parse for errors in MSBuild error format. 
        /// <seealso cref="http://blogs.msdn.com/b/msbuild/archive/2006/11/03/msbuild-visual-studio-aware-error-messages-and-message-formats.aspx"/>
        /// </param>
        public void ParseForTask(string tasksource, string line, Regex[] ignoreList)
        {
           this.Dispatcher.Invoke(new Action(() =>
           {
                TaskCategory category;
                string text, code, filename;
                int linenumber, column;

                if (MessageParsing.ParseErrorOrWarning(line, "", 0, ignoreList, out category, out text, out code, out filename, out linenumber, out column))
                {
                    AddTask(tasksource, category, text, code, filename, linenumber, column);
                }
           }));
        }

        /// <summary>
        /// Does the task list contains errors for the given task source.
        /// </summary>
        /// <param name="tasksource">True if it contains errors, false otherwise.</param>
        /// <returns></returns>
        public bool HasErrors(string tasksource)
        {
            return _tasks.Where(n => n.TaskSource == tasksource && n.Category == TaskCategory.Error).Count() > 0;
        }

        /// <summary>
        /// Get a report line on the number of warnings an errors for the given task source.
        /// </summary>
        /// <param name="tasksource">Task source to create report for.</param>
        /// <param name="canceled">The executing task is canceled by the user.</param>
        /// <returns></returns>
        public string GetTaskListReport(string tasksource, bool canceled)
        {
            int errors = _tasks.Where(n => n.TaskSource == tasksource && n.Category == TaskCategory.Error).Count();
            int warnings = _tasks.Where(n => n.TaskSource == tasksource && n.Category == TaskCategory.Warning).Count();

            string status = canceled? "CANCELED BY USER" : errors == 0 ? "succeeded" : "FAILED";
            string result = String.Format("========== Action: {0} with {1} error{2} and {3} warning{4} ==========",
                status, errors, errors == 1 ? "" : "s", warnings, warnings == 1 ? "" : "s");
            return result;
        }

        public void Show(string tasksource)
        {
            WebMatrixContext.EditorTaskPanelService.ShowBottomPane();
            WebMatrixContext.EditorTaskPanelService.ShowTaskTab(TaskListPane.TaskListPaneTaskPanelId);
            CurrentTaskSource = tasksource;
        }

        /// <summary>
        /// Function hooked up to double-click mouse event in TaskList.xaml when clicked on a task.
        /// Opens the warning/error file on the correct line and column.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void NavigateToTaskFile(object sender, MouseButtonEventArgs e)
        {
            var task = ((FrameworkElement)e.OriginalSource).DataContext as TaskItem;
            if (task != null)
            {
                // Construct error message, just in case
                string errortitle = "Can't open file";
                string errormessage = String.Format("Can't open the file '{0}'. Please navigate to the file manually and go to line {1} for the {2}.",
                    task.WorkspaceRelativeFilename, task.Linenumber, task.Category == TaskCategory.Error ? "error" : "warning");

                ICommand openFileCommand = WebMatrixContext.WebMatrixHost.HostCommands.OpenFileInEditor;
                if (File.Exists(task.Filename) && openFileCommand.CanExecute(task.Filename))
                {
                    try
                    {
                        openFileCommand.Execute(task.Filename);
                        IEditorWorkspace ws = (WebMatrixContext.WebMatrixHost.Workspace as IEditorWorkspace);
                        IEditorSelection editorSelection = ws.CurrentEditor.ServiceProvider.GetService(typeof(IEditorSelection)) as IEditorSelection;
                        int line = task.Linenumber > 0 ? task.Linenumber - 1 : 0;
                        int column = task.Column > 0 ? task.Column - 1 : 0;
                        editorSelection.GoTo(line, column, 0);
                    }
                    catch (Exception ex)
                    {
                        WebMatrixContext.WebMatrixHost.ShowExceptionMessage(errortitle, errormessage, ex);
                    }
                }
                else
                {
                    // TODO: would prefer to use MessageBoxButton.OK, but gives exception. WebMatrix 2 RC issue?
                    WebMatrixContext.WebMatrixHost.ShowDialog(errortitle, errormessage, DialogSize.Small, MessageBoxButton.OKCancel, MessageBoxResult.OK, null);
                }
            } 
        }

        /// <summary>
        /// Function hooked up to mouseup event in TaskList.xaml when clicked on the help icon of a task.
        /// Navigates to the help link in the currently selected task item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NavigateToHelp(object sender, MouseButtonEventArgs e)
        {
            var taskitem = ((FrameworkElement)e.OriginalSource).DataContext as TaskItem;
            if (taskitem != null && taskitem.HelpLink != null)
            {
                Process.Start(taskitem.HelpLink);
            }
        }

        private void TaskListClearButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Clear();
        }

        private void ShowErrorsCheckBox_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            CollectionViewSource vs = (CollectionViewSource)this.Resources["FilteredTasks"];
            vs.View.Refresh();
        }

        private void ShowWarningsCheckBox_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            CollectionViewSource vs = (CollectionViewSource)this.Resources["FilteredTasks"];
            vs.View.Refresh();
        }

        private void TaskSourceSelectorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CollectionViewSource vs = (CollectionViewSource)this.Resources["FilteredTasks"];
            vs.View.Refresh();
        }
    }
}
