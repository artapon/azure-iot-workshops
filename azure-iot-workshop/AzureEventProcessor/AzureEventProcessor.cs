using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Azure.NotificationHubs;

namespace AzureEventProcessor
{
    [DataContract]
    public class IoTHub
    {
        [DataMember]
        public object MessageId { get; set; }
        [DataMember]
        public object CorrelationId { get; set; }
        [DataMember]
        public string ConnectionDeviceId { get; set; }
        [DataMember]
        public string ConnectionDeviceGenerationId { get; set; }
        [DataMember]
        public string EnqueuedTime { get; set; }
        [DataMember]
        public object StreamId { get; set; }
    }

    [DataContract]
    public class ReceivedData
    {
        [DataMember] public string MessageType { get; set; }
        [DataMember]
        public object DeviceId { get; set; }
        [DataMember]
        public string TimeStamp { get; set; }
        [DataMember]
        public double Temperature { get; set; }
        [DataMember]
        public double Humidity { get; set; }
        [DataMember]
        public double WindSpeed { get; set; }
        [DataMember]
        public int Raining { get; set; }
        [DataMember]
        public string EventProcessedUtcTime { get; set; }
        [DataMember]
        public int PartitionId { get; set; }
        [DataMember]
        public string EventEnqueuedUtcTime { get; set; }
        [DataMember]
        public IoTHub IoTHub { get; set; }
    }

    public class AzureEventProcessor : IEventProcessor
    {
        private Stopwatch checkpointStopWatch;
        private ReceivedData receviedData;
        private NotificationHubClient hub; 

        async Task IEventProcessor.CloseAsync(PartitionContext context, CloseReason reason)
        {
            Debug.WriteLine($"Processor Shutting Down. Partition '{context.Lease.PartitionId}', Reason: '{reason}'.");
            if (reason == CloseReason.Shutdown)
            {
                await context.CheckpointAsync();
            }
        }

        private async Task SendNotificationAsync(string message)
        {
            var toast = @"<toast><visual><binding template=""ToastText01""><text id=""1"">Hello from a .NET App!</text></binding></visual></toast>";
            await hub.SendWindowsNativeNotificationAsync(toast);
        }

        Task IEventProcessor.OpenAsync(PartitionContext context)
        {
            hub = NotificationHubClient.CreateClientFromConnectionString("Endpoint=sb://labs-pushnotification-ns.servicebus.windows.net/;SharedAccessKeyName=DefaultFullSharedAccessSignature;SharedAccessKey=apZKIyGmyl9chsPtEhzpQIG58f7Bg3TbKEDjektwAvw=", "labs-pushnotification");

            Debug.WriteLine($"AzureEventProcessor initialized.  Partition: '{context.Lease.PartitionId}', Offset: '{context.Lease.Offset}'");
            checkpointStopWatch = new Stopwatch();
            checkpointStopWatch.Start();
            return Task.FromResult<object>(null);
        }

        async Task IEventProcessor.ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            foreach (EventData eventData in messages)
            {
                var data = Encoding.UTF8.GetString(eventData.GetBytes());
                receviedData = JsonConvert.DeserializeObject<ReceivedData>(data);
                Debug.WriteLine($"MessageType: {receviedData.MessageType}");
                Debug.WriteLine($"DeviceId: {receviedData.DeviceId}");
                Debug.WriteLine($"TimeStemp: {receviedData.TimeStamp}");
                Debug.WriteLine($"Temperature: {receviedData.Temperature}");
                Debug.WriteLine($"Humidity: {receviedData.Humidity}");
                Debug.WriteLine($"Wind Speed: {receviedData.WindSpeed}");
                Debug.WriteLine($"Raining: {receviedData.Raining}");
                Debug.WriteLine($"Event Enqueued UTC Time: {receviedData.EventEnqueuedUtcTime}");
                Debug.WriteLine($"Event Processed UTC Time: {receviedData.EventProcessedUtcTime}");
                Debug.WriteLine("");
                Console.WriteLine($"Message received.  Partition: '{context.Lease.PartitionId}', Data: '{data}'");

                await SendNotificationAsync("test");
                // do some Notification logic here...

            }

            //Call checkpoint every 5 minutes, so that worker can resume processing from 5 minutes back if it restarts.
            if (checkpointStopWatch.Elapsed > TimeSpan.FromMinutes(5))
            {
                await context.CheckpointAsync();
                checkpointStopWatch.Restart();
            }
        }
    }
}
