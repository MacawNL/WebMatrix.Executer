﻿// Implementing an Embeddeable type, see http://msdn.microsoft.com/en-us/library/dd409610.aspx.

using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.WebMatrix.Extensibility;
using Microsoft.WebMatrix.Extensibility.Editor;

namespace DesignFactory.WebMatrix.IExecuter
{
    [ComImport]
    [Guid("CCC20030-7F9E-4778-8E7D-E400DBC3661A")]
    public interface IExecuter
    {
        void Start(Action cancelAction);
        bool End(bool isCanceled);
        System.Threading.Tasks.Task<bool> RunAsync(string fileName, string arguments);
        System.Threading.Tasks.Task<bool> RunPowerShellAsync(string arguments);
        bool IsRunning();
        void Cancel();
        void Write(string format, params object[] args);
        void WriteLine(string format, params object[] args);
        void WriteNoParse(string format, params object[] args);
        void WriteLineNoParse(string format, params object[] args);
        void ConfigureParsing(Regex[] ignoreList, Func<string, string> processLineBeforeParsing);
        System.Windows.Threading.Dispatcher UIThreadDispatcher { get; }
 
        // Only used by DesignFactory.WebMatrix.ExecuterFactory.GetExecuter()
        void Initialize(string tasksource, IWebMatrixHost webMatrixHost, IEditorTaskPanelService editorTaskPanelService);
        void InitializeTabs();
    }
}
