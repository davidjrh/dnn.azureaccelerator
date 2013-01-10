using System;
using System.Diagnostics;
using System.ServiceModel;

namespace Microsoft.WindowsAzure.Accelerator.Diagnostics
{
    public interface ICloudTraceChannel : ICloudTraceContract, IClientChannel { }

    [ServiceContract(Name = "ICloudTraceContract", Namespace = "http://samples.microsoft.com/ServiceModel/Relay/CloudTrace", SessionMode = SessionMode.Allowed)]
    public interface ICloudTraceContract
    {
        [OperationContract(IsOneWay = true, Name = "Write1")]
        void Write(String message);

        [OperationContract(IsOneWay = true, Name = "Write2")]
        void Write(String message, String category);

        [OperationContract(IsOneWay = true, Name = "WriteLine1")]
        void WriteLine(String message);

        [OperationContract(IsOneWay = true, Name = "WriteLine2")]
        void WriteLine(String message, String category);

        [OperationContract(IsOneWay = true, Name = "Fail1")]
        void Fail(String message);

        [OperationContract(IsOneWay = true, Name = "Fail2")]
        void Fail(String message, String detailMessage);
    }

    [ServiceBehavior(Name = "CloudTraceService", Namespace = "http://samples.microsoft.com/ServiceModel/Relay/CloudTrace")]
    public class CloudTraceService : ICloudTraceContract
    {
        public void Write(String message)
        {
            //Write to the Console
            Console.Write(message);

            //Also write to a local Trace Listener
            Trace.Write(message);
        }

        public void Write(String message, String category)
        {
            //Write to the Console
            Console.Write(message, category);

            //Also write to a local Trace Listener
            Trace.Write(message, category);
        }

        public void WriteLine(String message)
        {
            //Write to the Console
            Console.WriteLine(message);

            //Also write to a local Trace Listener
            Trace.WriteLine(message);
        }

        public void WriteLine(String message, String category)
        {
            //Write to the Console
            Console.WriteLine(message, category);

            //Also write to a local Trace Listener
            Trace.WriteLine(message, category);
        }

        public void Fail(String message)
        {
            //Write to the Console
            Console.WriteLine(String.Format("Fail: {0}", message));

            //Also write to a local Trace Listener
            Trace.Fail(message);
        }

        public void Fail(String message, String detailMessage)
        {
            //Write to the Console
            Console.WriteLine(String.Format("Fail: {0}, {1}", message, detailMessage));

            //Also write to a local Trace Listener
            Trace.Fail(message, detailMessage);
        }
    }
}