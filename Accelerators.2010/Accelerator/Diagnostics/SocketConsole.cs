using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Microsoft.WindowsAzure.Accelerator.Diagnostics
{
    /// <summary>
    /// Remoted console window for multiple concurrent client connections.
    /// </summary>
    public class SocketConsole
    {
        private EventHandler _onClose;
        private SocketServer _socketServer;

        public enum InstanceType
        {
            Server,
            Client
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SocketConsole"/> class.
        /// </summary>
        /// <param name="serviceBusConnection">The service bus connection.</param>
        /// <param name="instanceType">Type of the instance.</param>
        public SocketConsole(ServiceBusConnection serviceBusConnection, InstanceType instanceType)
        {
            throw new NotImplementedException("ServiceBus based console not implemented. (i|rdm)");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SocketConsole"/> class.
        /// </summary>
        /// <param name="endPoint">The end point.</param>
        /// <param name="instanceType">Type of the instance.</param>
        public SocketConsole(IPEndPoint endPoint, InstanceType instanceType)
        {
            if ( instanceType == InstanceType.Server )
            {
                _socketServer = new SocketServer(endPoint);
                _onClose += (s,e) => _socketServer.Close();
            }
        }

        public void Close()
        {
            _onClose.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Standard TCP socket console server service.  (not Service Bus based, yet (i|rdm))
        /// </summary>
        private class SocketServer
        {
            private const Int32 _ConnectionTimeoutInSeconds = 6000;
            private readonly AutoResetEvent connectionWaitHandle = new AutoResetEvent(false);
            private TcpListener tcpListener;
            private Thread asyncListener;
            private StreamWriter RemoteConsoleWriter { get; set; }
            private Process ConsoleProcess { get; set; }

            public SocketServer(IPEndPoint endPoint)
            {
                try
                {
                    Trace.TraceInformation("SocketConsole : Configuration : {{[\"IP Address\"]:'{0}'}}, {{[\"Port\"]: '{1}'}}  ", endPoint.Address.ToString(), endPoint.Port.ToString());
                    tcpListener = new TcpListener(endPoint)
                                      {
                                          ExclusiveAddressUse = false
                                      };
                    tcpListener.Start();
                }
                catch ( SocketException se )
                {
                    Trace.TraceError("SocketConsole : Server could not start.\r\n{1}", se);
                    return;
                }

                asyncListener = new Thread(() =>
                                               {
                                                   try
                                                   {
                                                       while ( true )
                                                       {
                                                           IAsyncResult result = tcpListener.BeginAcceptTcpClient(HandleAsyncSocketServerConnection, tcpListener);
                                                           connectionWaitHandle.WaitOne();
                                                       }
                                                   }
                                                   catch ( Exception ex )
                                                   {
                                                       Trace.TraceError("SocketConsole : Exception in listener thread.\r\n{1}", ex.ToString());
                                                   }
                                               }

                    );
                asyncListener.Start();
            }

            /// <summary>
            /// Handles the async connection.
            /// </summary>
            /// <param name="result">The result.</param>
            private void HandleAsyncSocketServerConnection(IAsyncResult result)
            {
                // Accept connection
                var listener = (TcpListener)result.AsyncState;
                TcpClient netClient = listener.EndAcceptTcpClient(result);
                connectionWaitHandle.Set();

                // Accepted connection
                Guid clientId = Guid.NewGuid();
                netClient.ReceiveTimeout = _ConnectionTimeoutInSeconds * 1000;
                Trace.TraceInformation("SocketConsole : {0} : Accepted new connection.", clientId);

                // Setup reader/writer
                var netStream = netClient.GetStream();
                var netReader = new StreamReader(netStream);
                RemoteConsoleWriter = new StreamWriter(netStream) { AutoFlush = false };

                ConsoleProcess = new Process
                                             {
                                                 StartInfo =
                                                     {
                                                         FileName = "cmd.exe",
                                                         CreateNoWindow = true,
                                                         UseShellExecute = false,
                                                         //                  WindowStyle = ProcessWindowStyle.Hidden,
                                                         RedirectStandardOutput = true,
                                                         RedirectStandardInput = true,
                                                         RedirectStandardError = true
                                                     }
                                             };

                DataReceivedEventHandler outputHandler = (s, e) =>
                                                             {
                                                                 var consoleOutput = new StringBuilder();
                                                                 if ( !String.IsNullOrEmpty(e.Data) )
                                                                     try
                                                                     {
                                                                         consoleOutput.Append(e.Data);
                                                                         RemoteConsoleWriter.WriteLine(consoleOutput);
                                                                         RemoteConsoleWriter.Flush();
                                                                     }
                                                                     catch
                                                                     {
                                                                     }

                                                             };

                ConsoleProcess.OutputDataReceived += outputHandler;
                ConsoleProcess.ErrorDataReceived += outputHandler;

                ConsoleProcess.Start();
                ConsoleProcess.BeginErrorReadLine();
                ConsoleProcess.BeginOutputReadLine();
                Boolean continueProcessing = true;
                while ( continueProcessing )
                {
                    try
                    {
                        String consoleInput = netReader.ReadLine() ?? String.Empty;
                        if ( consoleInput.ToLower().StartsWith("wac") )
                            consoleInput = SocketConsoleHandler(consoleInput.Substring(3).TrimStart(' '));

                        ConsoleProcess.StandardInput.WriteLine(consoleInput);
                    }
                    catch ( Exception ex )
                    {
                        Trace.TraceError("SocketConsole : {0} : Error in console input.\r\n{1}", clientId, ex);
                        break;
                    }
                }
                Trace.TraceInformation("SocketConsole : {0} : Terminating console session.", clientId);
                ConsoleProcess.Protect(cp => cp.Kill());
                netReader.Close();
                RemoteConsoleWriter.Close();
                netStream.Close();
                netClient.Close();
            }

            /// <summary>
            /// Writes the string to the remote console output.
            /// </summary>
            /// <param name="output">The output.</param>
            private void WriteOutput(String output)
            {
                try
                {
                    Trace.TraceInformation("SocketConsole : Output\r\n{0}", output);
                    RemoteConsoleWriter.Write(output);
                    RemoteConsoleWriter.Flush();
                }
                catch
                {
                }
            }

            /// <summary>
            /// Stops the TCP listener.
            /// </summary>
            public void Close()
            {
                tcpListener.Protect(tl => tl.Stop());
            }

            /// <summary>
            /// Handles special socket console commands.
            /// </summary>
            /// <param name="consoleInput">The console input.</param>
            /// <param name="writer">The writer.</param>
            /// <returns></returns>
            private String SocketConsoleHandler(String consoleInput)
            {
                //throw new NotImplementedException();
                //i| Get all switches; then throw away the switch char in the resulting set of strings.
                var switches = consoleInput.Split('/', '-').Where(item => item.Length > 0).OnValid(items => items.ToList());

                StringBuilder remoteOutput = new StringBuilder();
                String localInput = null;
                if ( switches == null || switches.Count() < 1 || switches.Exists(s => s[0] == '?') || switches.Exists(s => s.ToLower() == "help") )
                {
                    WriteOutput("Usage: wac ...\n");
                }
                else if ( switches.Exists(s => Char.ToLower(s[0]) == 'r') )
                {
                    //i|
                    //i| Reset the entire accelerator.
                    //i| 
                    WriteOutput("\r\n[( Accelerator Reset Issued )]\r\n\r\nPerforming warm reset of accelerator...");
                    ServiceManager.Reset();
                    WriteOutput("\r\n[( Reset Complete )]\r\n");
                }

                RemoteConsoleWriter.Write(remoteOutput.ToString());

                return localInput ?? String.Empty;
            }
        }
    }
}