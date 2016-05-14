using System;
using Microsoft.ServiceBus.Messaging;

namespace AzureEventProcessor
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            const string eventHubConnectionString = "Endpoint=sb://labs-event-hub-ns.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=nWuOyVHIqCaG4pcuGLuOL2FPCtQjzUZReo4sXgLehDM=";
            const string eventHubName = "labs-event-hub";
            const string storageAccountName = "azureiotlabsstorage";
            const string storageAccountKey = "j96FwjBLcwHZzfWJ3go+72dWN0q3QxU817JJVReVCB6cexIr5SJ0cVpnaIwF0Ja6ozw21sKI5yuBdY7RVurbFg==";
            var storageConnectionString =$"DefaultEndpointsProtocol=https;AccountName={storageAccountName};AccountKey={storageAccountKey}";
            var eventProcessorHostName = Guid.NewGuid().ToString();

            var eventProcessorHost = new EventProcessorHost(eventProcessorHostName, eventHubName, EventHubConsumerGroup.DefaultGroupName, eventHubConnectionString, storageConnectionString);
            Console.WriteLine("Registering EventProcessor...");

            var options = new EventProcessorOptions();
            options.ExceptionReceived += (sender, e) => { Console.WriteLine(e.Exception); };
            eventProcessorHost.RegisterEventProcessorAsync<AzureEventProcessor>(options).Wait();
            Console.WriteLine("Receiving. Press enter key to stop worker.");
            Console.ReadLine();
            eventProcessorHost.UnregisterEventProcessorAsync().Wait();
        }
    }
}
