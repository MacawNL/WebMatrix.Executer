using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DesignFactory.WebMatrix.Executer
{
    public class TaskSourceOutput
    {
        /// <summary>
        /// Task source name.
        /// </summary>
        public string TaskSource { get; set; }

        /// <summary>
        /// The currently executing process, so we can cancel it
        /// </summary>
        public Process ExecutingProcess { get; set; }

        /// <summary>
        /// The Executer the currently running process is running under.
        /// </summary>
        public Executer ProcessExecuter { get; set; }

        /// <summary>
        /// Output generated for this task source.
        /// </summary>
        public string Output { get; set; }

        /// <summary>
        /// Equals override needed for XAML binding.
        /// </summary>
        /// <param name="obj">Object to compare to.</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is TaskSourceOutput)
            {
                return ((TaskSourceOutput)obj).TaskSource == TaskSource;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Get the Hash Code. Hash Code on TaskSource is good enough. Only one with Task Source name may exist.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return TaskSource.GetHashCode();
        }
    }
}
