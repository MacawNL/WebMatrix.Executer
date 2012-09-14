using System;
using System.Diagnostics;

namespace DesignFactory.WebMatrix.Executer
{
    public class ProcessOutputHandler
    {
        public System.Diagnostics.Process _proc { get; set; }
        private string _tasksource;
        private Executer _executer;

        /// <summary>       
        /// The constructor requires a reference to the process that will be read.
        /// The process should have .RedirectStandardOutput and .RedirectStandardError set to true.
        /// </summary>
        /// <param name="tasksource">Unique name of the executing task.</param>
        /// <param name="process">The process that will have its output read by this class.</param>
        /// <param name="worker"></param>
        public ProcessOutputHandler(string tasksource, Executer executer, System.Diagnostics.Process process)
        {
            _tasksource = tasksource;
            _executer = executer;
            _proc = process;
            Debug.Assert(_proc.StartInfo.RedirectStandardError,
                         "RedirectStandardError must be true to use ProcessOutputHandler.");
            Debug.Assert(_proc.StartInfo.RedirectStandardOutput,
                         "RedirectStandardOut must be true to use ProcessOutputHandler.");
        }

        /// <summary>
        /// This method starts reading the standard error stream from Process.
        /// </summary>       
        public void ReadStdErr()
        {
            string line;
            try
            {
                while ((line = _proc.StandardError.ReadLine()) != null)
                {
                    _executer.WriteLine(line);
                    WebMatrixContext.TaskListPaneInstance.ParseForTask(_tasksource, line);
                }
            }
            catch(Exception ex)
            {
                _executer.WriteLine("Reading from stderr failed. Exception: {0}", ex.ToString());
            }
        }

        /// <summary>
        /// This method starts reading the standard output stream from Process.
        /// </summary>
        public void ReadStdOut()
        {
            string line;
            try
            {
                while ((line = _proc.StandardOutput.ReadLine()) != null)
                {
                    _executer.WriteLine(line);
                    WebMatrixContext.TaskListPaneInstance.ParseForTask(_tasksource, line);
                }
            }
            catch (Exception ex)
            {
                _executer.WriteLine("Reading from stdout failed. Exception: {0}", ex.ToString());
            }
        }
    }
}