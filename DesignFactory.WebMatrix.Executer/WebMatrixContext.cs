using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.WebMatrix.Extensibility;
using Microsoft.WebMatrix.Extensibility.Editor;

namespace DesignFactory.WebMatrix.Executer
{
    public class WebMatrixContext
    {
        public static void Initialize(IWebMatrixHost webMatrixHost, IEditorTaskPanelService editorTaskPanelService)
        {
            if (webMatrixHost == null || editorTaskPanelService == null)
            {
                MessageBox.Show("Extension not working correctly. Extension must call DesignFactory.WebMatrix.Executer.Initialize() function correctly. WebMatrixHost or EditorTaskPanelService is null.", "Extension failure", MessageBoxButton.OK);
            }
            else
            {
                if (!_isInitialised)
                {
                    WebMatrixHost = webMatrixHost;
                    EditorTaskPanelService = editorTaskPanelService;
                    OutputPaneInstance = new OutputPane();
                    TaskListPaneInstance = new TaskListPane();
                    _isInitialised = true;
                }
            }
        }

        public static void SetCancelAction(Action cancelAction)
        {
            OutputPaneInstance.CancelAction = cancelAction;
        }

        public static void InitializeTabs()
        {
            if (WebMatrixContext.WebMatrixHost.WebSite != null)
            {
                if (!WebMatrixContext.EditorTaskPanelService.TaskTabExists(OutputPane.OutputPaneTaskPanelId)) 
                {
                    WebMatrixContext.EditorTaskPanelService.AddTaskTab(OutputPane.OutputPaneTaskPanelId, new TaskTabItemDescriptor(null, "Output", WebMatrixContext.OutputPaneInstance, Brushes.DarkOliveGreen));
                }
                if (!WebMatrixContext.EditorTaskPanelService.TaskTabExists(TaskListPane.TaskListPaneTaskPanelId))
                {
                    WebMatrixContext.EditorTaskPanelService.AddTaskTab(TaskListPane.TaskListPaneTaskPanelId, new TaskTabItemDescriptor(null, "Errors & Warnings", WebMatrixContext.TaskListPaneInstance, Brushes.DarkOliveGreen));
                }
            }
 
            else 
            {
                if (WebMatrixContext.EditorTaskPanelService.TaskTabExists(OutputPane.OutputPaneTaskPanelId)) 
                {
                    WebMatrixContext.EditorTaskPanelService.RemoveTaskTab(OutputPane.OutputPaneTaskPanelId);
                }
                if (WebMatrixContext.EditorTaskPanelService.TaskTabExists(TaskListPane.TaskListPaneTaskPanelId)) 
                {
                    WebMatrixContext.EditorTaskPanelService.RemoveTaskTab(TaskListPane.TaskListPaneTaskPanelId); 
                }
            }
        }
        // Only the first extension that is executed will do initialization.
        internal static bool _isInitialised = false;

        public static IWebMatrixHost WebMatrixHost { get; private set; }
        public static IEditorTaskPanelService EditorTaskPanelService { get; private set; }
        
        // Output pane and task list pane are only accessible from within this assembly, because multiple plugins can use the functionality
        internal static OutputPane OutputPaneInstance { get; private set; }
        internal static TaskListPane TaskListPaneInstance { get; private set; }

        /// <summary>
        /// Given an absolute path to a file, give the workspace relative location for display. Starts with ~ to specify site root.
        /// </summary>
        /// <param name="filename">The absolute path to a file.</param>
        /// <returns>A workspace relative path for display purposes.</returns>
        public static string GetWorkspaceRelativeFilename(string filename)
        {
            bool? islocal = WebMatrixContext.WebMatrixHost.WebSite.IsLocal;
            string result = filename;
            if (islocal == true)
            {
                string localWebSitePath = WebMatrixContext.WebMatrixHost.WebSite != null ? WebMatrixContext.WebMatrixHost.WebSite.Path : "<NOPATH>";
                if (filename.StartsWith(localWebSitePath)) // can't use null, path always starts with null
                {
                    result = "~" + filename.Substring(localWebSitePath.Length);
                }
            }

            return result;
        }
    }
}
