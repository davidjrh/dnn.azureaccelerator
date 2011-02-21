using System;
using System.Diagnostics;
using System.ServiceModel;
using Microsoft.ServiceBus;

namespace Microsoft.WindowsAzure.Accelerator.Diagnostics
{
    /// <summary>
    /// Windows Azure Accelerators trace and debug output listener class.
    /// </summary>
    public class CloudTraceListener : TraceListener
    {
        private const Int32 MaxRetries = 1;
        private const Int32 BackoffSeconds = 60;

        private Object _writeMutex = new Object();
        private DateTime _backoffUntil = DateTime.Now;
        private ChannelFactory<ICloudTraceChannel> _traceChannelFactory;
        private ICloudTraceChannel _cloudTraceChannel;
        private Boolean _backoff = false;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CloudTraceListener"/> class.
        /// </summary>
        public CloudTraceListener() 
            : this(ServiceBusConnection.FromConfigurationSetting("DiagnosticsServiceBus"))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudTraceListener"/> class.
        /// </summary>
        /// <param name="servicePath">The service path.</param>
        /// <param name="serviceNamespace">The service namespace.</param>
        /// <param name="issuerName">Name of the issuer.</param>
        /// <param name="issuerSecret">The issuer secret.</param>
        public CloudTraceListener(String servicePath, String serviceNamespace, String issuerName, String issuerSecret) 
            : this(new ServiceBusConnection(serviceNamespace, servicePath, issuerName, issuerSecret))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudTraceListener"/> class.
        /// </summary>
        /// <param name="serviceBusConnection">The service bus connection.</param>
        public CloudTraceListener(ServiceBusConnection serviceBusConnection)
        {
            _traceChannelFactory = new ChannelFactory<ICloudTraceChannel>(new NetEventRelayBinding(), new EndpointAddress(serviceBusConnection.GetServiceUri()));
            _traceChannelFactory.Endpoint.Behaviors.Add(serviceBusConnection.GetTransportClientEndpointBehavior());
        }

        /// <summary>
        /// Gets a value indicating whether the trace listener is thread safe.
        /// </summary>
        /// <value></value>
        /// <returns><see langword="true"/> if the trace listener is thread safe; otherwise, <see langword="false"/>. The default is <see langword="false"/>.
        /// </returns>
        public override Boolean IsThreadSafe
        {
            get { return true; }
        }

        /// <summary>
        /// When overridden in a derived class, closes the output stream so it no longer receives tracing or debugging output.
        /// </summary>
        public override void Close()
        {
            _cloudTraceChannel.Close();
            _traceChannelFactory.Close();
        }

        /// <summary>
        /// Locks the wrapper.
        /// </summary>
        /// <param name="action">The action.</param>
        private void LockWrapper(Action action)
        {
            try
            {
                lock (_writeMutex)
                {
                    //i| Test if we are writing data: we stop trying to write for a period if there was no server is listening to receive data.
                    if (_backoff)
                    {
                        if (DateTime.Now < _backoffUntil)
                            return;
                        _backoff = false;
                    }
                    Int32 retry = 0;
                    for (;;)
                    {
                        //i| Test for valid Service Bus connection; otherwise backoff.
                        EnsureChannel();
                        try
                        {
                            //i| Channel verified; proceed with service bus communication.
                            action.Invoke();
                            return;
                        }
                        //x| catch (CommunicationException ex)
                        catch 
                        {
                            if (++retry > MaxRetries)
                            {
                                _backoff = true;
                                _backoffUntil = DateTime.Now.AddSeconds(BackoffSeconds);
                                return;
                            }
                        }
                    }
                }
            }
            //catch (EndpointNotFoundException ee) { }
            //catch (CommunicationException ce) { }
            catch
            {
                //i| Server not available; stop logging attempts for backoff seconds.
                _backoff = true;
                _backoffUntil = DateTime.Now.AddSeconds(BackoffSeconds);
                Trace.TraceError("TraceListener : Error opening connection to listener service console.");
                Trace.TraceError("TraceListener : Realtime logging suspended for {0} seconds.", BackoffSeconds);
            }
        }

        /// <summary>
        /// When overridden in a derived class, writes the specified message to the listener you create in the derived class.
        /// </summary>
        /// <param name="message">A message to write.</param>
        public override void Write(String message)
        {
            LockWrapper(() => _cloudTraceChannel.Write(message));
        }

        /// <summary>
        /// Writes the value of the object's <see cref="M:System.Object.ToString"/> method to the listener you create when you implement the <see cref="T:System.Diagnostics.TraceListener"/> class.
        /// </summary>
        /// <param name="o">An <see cref="T:System.Object"/> whose fully qualified class name you want to write.</param>
        public override void Write(Object o)
        {
            LockWrapper(() => _cloudTraceChannel.Write(o.ToString()));
        }

        /// <summary>
        /// Writes a category name and the value of the object's <see cref="M:System.Object.ToString"/> method to the listener you create when you implement the <see cref="T:System.Diagnostics.TraceListener"/> class.
        /// </summary>
        /// <param name="o">An <see cref="T:System.Object"/> whose fully qualified class name you want to write.</param>
        /// <param name="category">A category name used to organize the output.</param>
        public override void Write(Object o, String category)
        {
            LockWrapper(() => _cloudTraceChannel.Write(o.ToString(), category));
        }

        /// <summary>
        /// Writes a category name and a message to the listener you create when you implement the <see cref="T:System.Diagnostics.TraceListener"/> class.
        /// </summary>
        /// <param name="message">A message to write.</param>
        /// <param name="category">A category name used to organize the output.</param>
        public override void Write(String message, String category)
        {
            LockWrapper(() => _cloudTraceChannel.Write(message, category));
        }

        /// <summary>
        /// When overridden in a derived class, writes a message to the listener you create in the derived class, followed by a line terminator.
        /// </summary>
        /// <param name="message">A message to write.</param>
        public override void WriteLine(String message)
        {
            LockWrapper(() => _cloudTraceChannel.WriteLine(message));
        }

        /// <summary>
        /// Writes the value of the object's <see cref="M:System.Object.ToString"/> method to the listener you create when you implement the <see cref="T:System.Diagnostics.TraceListener"/> class, followed by a line terminator.
        /// </summary>
        /// <param name="o">An <see cref="T:System.Object"/> whose fully qualified class name you want to write.</param>
        public override void WriteLine(Object o)
        {
            LockWrapper(() => _cloudTraceChannel.WriteLine(o.ToString()));
        }

        /// <summary>
        /// Writes a category name and the value of the object's <see cref="M:System.Object.ToString"/> method to the listener you create when you implement the <see cref="T:System.Diagnostics.TraceListener"/> class, followed by a line terminator.
        /// </summary>
        /// <param name="o">An <see cref="T:System.Object"/> whose fully qualified class name you want to write.</param>
        /// <param name="category">A category name used to organize the output.</param>
        public override void WriteLine(Object o, String category)
        {
            LockWrapper(() => _cloudTraceChannel.WriteLine(o.ToString(), category));
        }

        /// <summary>
        /// Writes a category name and a message to the listener you create when you implement the <see cref="T:System.Diagnostics.TraceListener"/> class, followed by a line terminator.
        /// </summary>
        /// <param name="message">A message to write.</param>
        /// <param name="category">A category name used to organize the output.</param>
        public override void WriteLine(String message, String category)
        {
            LockWrapper(() => _cloudTraceChannel.WriteLine(message, category));
        }

        /// <summary>
        /// Emits an error message to the listener you create when you implement the <see cref="T:System.Diagnostics.TraceListener"/> class.
        /// </summary>
        /// <param name="message">A message to emit.</param>
        public override void Fail(String message)
        {
            LockWrapper(() => _cloudTraceChannel.Fail(message));
        }

        /// <summary>
        /// Emits an error message and a detailed error message to the listener you create when you implement the <see cref="T:System.Diagnostics.TraceListener"/> class.
        /// </summary>
        /// <param name="message">A message to emit.</param>
        /// <param name="detailMessage">A detailed message to emit.</param>
        public override void Fail(String message, String detailMessage)
        {
            LockWrapper(() => _cloudTraceChannel.Fail(message, detailMessage));
        }

        /// <summary>
        /// Ensures the trace communication channel.
        /// </summary>
        private void EnsureChannel()
        { 
            if ( _cloudTraceChannel == null || _cloudTraceChannel.State != CommunicationState.Opened )
            {
                _cloudTraceChannel.OnValid(c => c.Abort());
                _cloudTraceChannel = _traceChannelFactory.CreateChannel();
                _cloudTraceChannel.Open();
            }
        }
    }
}