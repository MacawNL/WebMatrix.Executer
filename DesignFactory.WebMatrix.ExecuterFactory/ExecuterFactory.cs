using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.WebMatrix.Extensibility;
using Microsoft.WebMatrix.Extensibility.Editor;

namespace DesignFactory.WebMatrix
{
    public static class ExecuterFactory
    {
        public static DesignFactory.WebMatrix.IExecuter.IExecuter GetExecuter(string tasksource, IWebMatrixHost webMatrixHost, IEditorTaskPanelService editorTaskPanelService)
        {
            string newestAssemblyFile = null;
            Version newestAssemblyVersion = null;

            string webMatrixExtensionsFolder = Path.GetDirectoryName(Path.GetDirectoryName(new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath));
            var assemblyFiles = Directory.EnumerateFiles(webMatrixExtensionsFolder, "DesignFactory.WebMatrix.Executer.dll", SearchOption.AllDirectories);
            foreach (string currentAssemblyFile in assemblyFiles)
            {
                AssemblyName currentAssemblyName = AssemblyName.GetAssemblyName(currentAssemblyFile);
                if (newestAssemblyFile == null || currentAssemblyName.Version.CompareTo(newestAssemblyVersion) > 0)
                {
                    newestAssemblyFile = currentAssemblyFile;
                    newestAssemblyVersion = currentAssemblyName.Version;
                }
            }

            if (newestAssemblyFile == null)
            {
                return null;
            }

            // If an assembly with the same identity is already loaded, LoadFrom returns the loaded assembly even if a different path was specified.
            // http://msdn.microsoft.com/en-us/library/1009fa28.aspx
            Assembly executerAssembly = Assembly.LoadFrom(newestAssemblyFile);

            DesignFactory.WebMatrix.IExecuter.IExecuter executer = (DesignFactory.WebMatrix.IExecuter.IExecuter)executerAssembly.CreateInstance("DesignFactory.WebMatrix.Executer.Executer");
            if (executer == null)
            {
                throw new Exception(String.Format("Failed to instantiate Executer functionality on DesignFactory.WebMatrix.Executer assembly.\nAssembly={0}\nVersion={1}", newestAssemblyFile, newestAssemblyVersion));
            }
            else
            {
                executer.Initialize(tasksource, webMatrixHost, editorTaskPanelService);

                // Mandatory to get the tab showing on changes!
                editorTaskPanelService.PageChanged += (object sender, EventArgs eventArgs) => { executer.InitializeTabs(); };

                // Mandatory to get tabs displayed directly!
                executer.InitializeTabs();
            }
            return executer;
        }

    }
}
