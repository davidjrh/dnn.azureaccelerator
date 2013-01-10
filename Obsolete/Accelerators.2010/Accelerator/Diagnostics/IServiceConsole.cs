using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.ServiceBus;
using Microsoft.WindowsAzure.Diagnostics;
using System;
using System.ServiceModel;

namespace Microsoft.WindowsAzure.Accelerator.Diagnostics
{   
    /// <summary>
    /// Defines the WCF interface for the ServiceConsole service bus communication.
    /// </summary>
    [ServiceContract(Name = "IServiceConsole", Namespace = "http://samples.microsoft.com/ServiceModel/Relay/")]
    public interface IServiceConsole
    {
        [OperationContract]
        String Console(String text);
    }
    
    /// <summary>
    /// Defines the behavior of outbound request and request/reply channels used by client applications.
    /// </summary>
    public interface IServiceConsoleChannel : IServiceConsole, IClientChannel { }


    /// <summary>
    /// ServiceConsole service bus communication.
    /// </summary>
    [ServiceBehavior(Name = "ServiceConsole", Namespace = "http://samples.microsoft.com/ServiceModel/Relay/")]
    public class ServiceConsole : IServiceConsole
    {
        private const Int32 _BufferSize = 2048;
        
#region | PROPERTIES
        
        public Process Process { get; set; }
        public Boolean Active { get; set; }
        public String Output { get; set; }

#endregion
#region | METHODS

        /// <summary>
        /// Handles the console message event.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        public String Console(String text)
        {
            Trace.TraceInformation(String.Format("Console Text: {0}", text));
            return String.IsNullOrEmpty(text) ? String.Empty : RunCommand(text);
        }

        /// <summary>
        /// Runs the command in a console and returns the output.
        /// </summary>
        /// <returns>Console output.</returns>
        public String RunCommand(String command)
        {
            Process = new Process();
            try
            {
                //i| Setting the required properties for the process.
                Process.StartInfo.RedirectStandardOutput = true;
                Process.StartInfo.RedirectStandardError = true;
                Process.StartInfo.RedirectStandardInput = true; //bugbug| this might be wrong
                Process.StartInfo.UseShellExecute = false;
                Process.StartInfo.CreateNoWindow = true;
                Process.EnableRaisingEvents = false; 
                Process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                //i| Set the process filename and args.
                Process.StartInfo.FileName = "cmd.exe";
                Process.StartInfo.Arguments = String.Format("/C {0}", command);
                
                //i| Set the environment.
                //foreach (var v in ServiceManager.EnvironmentVariables)
                //    Process.StartInfo.EnvironmentVariables[v.Key] = v.Value;

                //i| Starting the Process
                Active = false;
                Output = String.Empty;
                var thread = new Thread((x) =>
                                            {
                                                while ( Active )
                                                {
                                                    var buffer = new Char[_BufferSize];
                                                    var sr = Process.StandardOutput;
                                                    var count = sr.Read(buffer, 0, _BufferSize - 1);
                                                    using ( var sw = new StringWriter() )
                                                    {
                                                        sw.Write(buffer, 0, count);
                                                        Output = Output + sw;
                                                    }
                                                }
                                            });
                Process.Start();
                thread.Start();
                Process.WaitForExit(0x2710);
                Active = false;
                thread.Join();
                using ( var sr = Process.StandardOutput )
                    Output = Output + sr.ReadToEnd();
            }
            catch
            {
                Debug.Assert(true);
            }
            return Output;
        }

#endregion
#region | TEST FACTORY

        public static void RunServerAndClient(ServiceBusConnection connection) //String servicePath, String serviceNamespace, String issuerName, String issuerSecret)
        {
            //CreateService(servicePath, serviceNamespace, issuerName, issuerSecret);
            CreateClientConsole(connection); //servicePath, serviceNamespace, issuerName, issuerSecret);
        }

#endregion
#region | CLIENT FACTORY

        /// <summary>
        /// Creates the service console client.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public static void CreateClientConsole(ServiceBusConnection connection)
        {
            ChannelFactory<IServiceConsoleChannel> channelFactory = null;
            IServiceConsoleChannel channel = null;
            TextWriter originalConsoleOut = System.Console.Out;
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);
            System.Console.SetOut(sw);

            try
            {
                //i| Debug trace.
                System.Console.WriteLine("Remote service connection point is '{0}'.", connection.GetServiceUri());

                //i| Create channel factory.
                channelFactory = connection.CreateChannelFactory<IServiceConsoleChannel>(new NetTcpRelayBinding());
                channel = channelFactory.CreateChannel();

                String consoleInput = String.Empty;
                Int32 currentStateIterations = 0;
                CommunicationState currentState = CommunicationState.Closed;
              
                do
                {
                    //i| Test for channel state change.
                    CommunicationState state;
                    if ((state = channel.State) != currentState)
                    {
                        System.Console.WriteLine("Communication status is now '{0}'.", state);
                        currentState = state;
                        currentStateIterations = 0;
                    }
                    else
                    {
                        currentStateIterations++;
                    }

                    //i| Act on channel state.
                    switch (currentState)
                    {
                        case CommunicationState.Closed:
                            Thread.Sleep(5000);
                            System.Console.Write(currentStateIterations == 0 ? "Attempting to open connection..." : ".");
                            channel = channelFactory.CreateChannel();
                            continue;
                        case CommunicationState.Faulted:
                            System.Console.Write(currentStateIterations == 0
                                                     ? "Attempting to recover connection..."
                                                     : ".");
                            channel = channelFactory.CreateChannel();
                            Thread.Sleep(5000);
                            continue;
                        case CommunicationState.Opening:
                            Thread.Sleep(1000);
                            continue;
                        case CommunicationState.Closing:
                            Thread.Sleep(5000);
                            continue;
                        case CommunicationState.Created:
                            try
                            {
                                channel.Open(TimeSpan.FromSeconds(30));
                            }
                            catch
                            {
                                System.Console.WriteLine("Unable to open channel; press Enter to try again.");
                            }
                            continue;
                        case CommunicationState.Opened:
                            System.Console.Write(currentStateIterations == 0
                                                     ? "Connected to server. Type your command; press Enter to send.\r\n\r\n> "
                                                     : "\r\n> ");
                            break;
                    }

                    //i| Valid connection, so query the user for commands.
                    consoleInput = System.Console.ReadLine();
                    LogLevel.Information.TraceContent("ServiceConsole", consoleInput, "Sending...");
                    Trace.WriteLine(consoleInput);
                    try
                    {
                        String consoleOutput = channel.Console(consoleInput);
                        LogLevel.Information.TraceContent("ServiceConsole", consoleOutput, "Received...");
                        System.Console.Write(consoleOutput);
                    }
                    catch
                    {
                    }

                } while (consoleInput != null && !consoleInput.Contains("\x3"));

                LogLevel.Information.Trace("ServiceConsole", "Closing channel.");
                channel.Close();
                LogLevel.Information.Trace("ServiceConsole", "Closing factory.");
                channelFactory.Close();
            }
            catch ( Exception ex )
            {
                LogLevel.Error.TraceException("ServiceConsole", ex, "{0}", ex.Message);
                System.Console.WriteLine("An unrecoverable error occurred: '{0}'.\r\n\r\nPress enter to exit.", ex.Message);
                System.Console.ReadLine();
                //i| Gracefully close the channel and factory.
                if ( channel != null )
                    CloseCommunicationObject(channel);
                if ( channelFactory != null )
                    CloseCommunicationObject(channelFactory);
            }
            finally
            {
                System.Console.SetOut(originalConsoleOut);
            }
        }

        private static void CloseCommunicationObject(ICommunicationObject communicationObject)
        {
            Boolean shouldAbort = true;
            if ( communicationObject.State == CommunicationState.Opened )
                try {
                    communicationObject.Close();
                    shouldAbort = false;
                }
                catch ( TimeoutException ) {}
                catch ( CommunicationException ) {}
            if (shouldAbort) communicationObject.Abort();
        }

#endregion
#region | SERVICE FACTORY

        /// <summary>
        /// Initializes the service console service.
        /// </summary>
        public static ServiceHost CreateService(ServiceBusConnection connection)
        {
            LogLevel.Information.Trace("");
            var host = connection.CreateServiceHost(typeof(ServiceConsole), typeof(IServiceConsole), new NetTcpRelayBinding());

            //i| Open the host connection.
            try {
                host.Open();
            }
            catch {
                //host.OnValid(h => h.Close());
                host = null;
            }
            return host;
        }

#endregion
    }
}
