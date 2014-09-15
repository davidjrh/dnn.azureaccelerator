using System;
using System.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace DotNetNuke.Azure.Accelerator.Plugins
{
    public class ExternalEventTasks: PluginBase
    {
        public override void OnStart()
        {
            ExecuteEventTask("OnStart");
        }

        public override void OnStop()
        {
            ExecuteEventTask("OnStop");
        }

        public override void OnSiteReady()
        {
            ExecuteEventTask("OnSiteReady");
        }

        /// <summary>
        /// Executes the event task.
        /// </summary>
        /// <param name="eventName">Name of the event.</param>
        private void ExecuteEventTask(string eventName)
        {
            try
            {
                string error;
                Trace.TraceInformation("Calling external event tasks ({0})...", eventName);
                int exitCode = ExecuteCommand(@"scripts\RunExternalEventTasks.cmd", eventName, out error, 30000);
                if (exitCode != 0)
                {
                    Trace.TraceWarning("Error while calling external event tasks ({0}): {1}", eventName, error);
                }
            }
            catch (Exception e)
            {
                Trace.TraceError("ERROR while calling event task {0}: {1}{2}Stack trace: {3}", eventName, e.Message, Environment.NewLine, e.StackTrace);
            }            
        }

        #region ExecuteCommand

        /// <summary>
        /// Executes an external .exe command
        /// </summary>
        /// <param name="exe">EXE path</param>
        /// <param name="arguments">Arguments</param>
        /// <param name="output">Output of the command</param>
        /// <param name="error">Contents of the error results if fails</param>
        /// <param name="timeout">Timeout for executing the command in milliseconds</param>
        /// <returns>Exit code</returns>
        public static int ExecuteCommand(string exe, string arguments, out string output, out string error, int timeout)
        {
            var p = new Process
            {
                StartInfo =
                {
                    FileName = exe,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };
            p.Start();
            error = p.StandardError.ReadToEnd();
            output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(timeout);
            int exitCode = p.ExitCode;
            p.Close();

            return exitCode;
        }

        /// <summary>
        /// Executes an external .exe command
        /// </summary>
        /// <param name="exe">EXE path</param>
        /// <param name="arguments">Arguments</param>
        /// <param name="error">Contents of the error results if fails</param>
        /// <param name="timeout">Timeout for executing the command in milliseconds</param>
        /// <param name="traceError">Trace error if the exit code is not zero</param>
        /// <returns>Exit code</returns>
        public static int ExecuteCommand(string exe, string arguments, out string error, int timeout, bool traceError = true)
        {
            string output;
            int exitCode = ExecuteCommand(exe, arguments, out output, out error, timeout);
            if (exitCode != 0 && traceError)
            {
                Trace.TraceWarning("Error executing command: {0}", error);
            }
            return exitCode;
        }


        #endregion

    }
}
