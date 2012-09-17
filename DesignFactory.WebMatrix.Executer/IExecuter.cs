using System;
using System.Runtime.InteropServices;

namespace DesignFactory.WebMatrix.Executer
{
    [ComImport]
    [Guid("CCC20030-7F9E-4778-8E7D-E400DBC3661A")]
    class IExecuter
    {
        void Cancel();
        bool IsRunning();
        System.Threading.Tasks.Task<bool> RunAsync(string fileName, string arguments);
        System.Threading.Tasks.Task<bool> RunPowerShellAsync(string arguments);
        void ShowOutputPaneAndClearTaskListPane();
        void Write(string format, params object[] args);
        void WriteLine(string format, params object[] args);
}
