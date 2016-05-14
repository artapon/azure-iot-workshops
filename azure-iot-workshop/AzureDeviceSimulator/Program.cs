using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

namespace AzureDeviceSimulator
{
    internal class Program
    {
        private static Random random;
        private static bool isConnected;
        private static DeviceClient deviceClient;
        private static bool isListening;

        private static void Main(string[] args)
        {
            random = new Random();
            var userInput = -1;
            string connectionString = null;
            string deviceId = null;
            string hostName = null;
            string deviceKey = null;
#if DEBUG
            connectionString = "HostName=labs-iot-hub.azure-devices.net;DeviceID=device-001;SharedAccessKey=nEGambHYUyyVgF/FmZoW6eAGVQszuUJbus7IRWVNPMA=";
#else
            Console.Write("Enter Device ID: ");
            deviceId = Console.ReadLine();
            deviceId = "device-001";

            Console.Write("Enter HostName: ");
            hostName = Console.ReadLine();

            Console.Write("Enter Device Key: ");
            deviceKey = Console.ReadLine();
            connectionString = $"HostName={hostName};DeviceID={deviceId};SharedAccessKey={deviceKey}";
#endif

            do
            {
                switch (UserPrompt())
                {
                    case 0:
                        userInput = 0;
                        DisconnectFromAzureIoTHubAsync().Wait();
                        break;
                    case 1:
                        var sensorData = GetSensorData(deviceId);
                        SendTelemetryData(sensorData).Wait();
                        break;
                    case 2:
                        SendBatchTelemetry(deviceId).Wait();
                        break;
                    case 3:
                        ConnectToAzureIoTHubAcync(connectionString).Wait();
                        break;
                    case 4:
                        Task.Run(async () => await RecieveMessageFromCloudAsync());
                        break;
                    case 5:
                        DisconnectFromAzureIoTHubAsync().Wait();
                        break;
                    default:
                        userInput = -1;
                        break;
                }
                Console.WriteLine(isConnected
                    ? "Connected to Azure IoT Hub."
                    : "Please connect to Azure IoT Hub before sending data (select 2 to connect)");
            } while (userInput != 0);
        }

        private static int UserPrompt()
        {
            var userInput = 0;
            Console.WriteLine(" 1) to Send a telemetry");
            Console.WriteLine(" 2) to Send X number of telemetries");
            Console.WriteLine(" 3) to Connect to IoT Hub");
            Console.WriteLine(" 4) to listen to Cloud messages");
            Console.WriteLine(" 5) to Disconnect from IoT Hub");
            Console.WriteLine(" 0) to exit");
            Console.Write("Enter your choice: ");
            return int.TryParse(Console.ReadLine(), out userInput) ? userInput : -1;
        }

        private static async Task SendBatchTelemetry(string deviceId)
        {
            Console.Write("Number of Telemetries to be sent: ");
            var tries = 0;
            if (int.TryParse(Console.ReadLine(), out tries))
            {
                var i = 0;
                for (i = 0; i < tries; i++)
                {
                    await SendTelemetryData(GetSensorData(deviceId));
                }
                Console.WriteLine($"{i} telemetries sent to Azure IoT Hub...");
            }
            else
            {
                Console.WriteLine("Invaid entries....");
            }
        }

        private static async Task SendTelemetryData(string telemetryJson)
        {
            if (telemetryJson == null) throw new ArgumentNullException(nameof(telemetryJson));
            if (isConnected)
            {
                try
                {
                    var message = new Message(Encoding.UTF8.GetBytes(telemetryJson));
                    await deviceClient.SendEventAsync(message);
                    Console.WriteLine($"SENT: {ConsoleColor.Red}{telemetryJson}");
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred while sending telemetry data to Azure IoT Hub: " + e.Message);
                }
            }
            else
            {
                Console.WriteLine(
                    "Connection to IoT Hub has NOT been established.  Please connect before sending the data.");
            }
        }

        private static string GetSensorData(string deviceId)
        {
            var temperature = random.NextDouble()*25 + 4;
            var humidity = random.NextDouble()*55 + 4;
            var windSpeed = random.NextDouble()*25 + 5;
            var raning = random.Next(0, 1) > 0;

            var telemetry = new
            {
                DeviceId = deviceId,
                TimeStamp = DateTime.Now,
                Temperature = temperature,
                Humidity = humidity,
                WindSpeed = windSpeed,
                Raining = raning
            };
            return JsonConvert.SerializeObject(telemetry);
        }

        private static async Task DisconnectFromAzureIoTHubAsync()
        {
            if (!isConnected) return;
            try
            {
                await deviceClient.CloseAsync();
                deviceClient = null;
                Console.WriteLine("The connection to Azure IoT Hub has been successfully closed.");
            }
            catch (Exception e)
            {
                deviceClient = null;
                Console.WriteLine("An error occurred while closing connection to the Hub: " + e.Message);
            }
            isConnected = false;
        }

        private static async Task RecieveMessageFromCloudAsync()
        {
            if (deviceClient != null)
            {
                isListening = true;
                Console.WriteLine("Listening to the Cloud for messages....");
                while (true)
                {
                    var receivedMessage = await deviceClient.ReceiveAsync();
                    if (receivedMessage == null) continue;

                    var command = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Received message: {command}");
                    Console.ResetColor();
                    await deviceClient.CompleteAsync(receivedMessage);

                    if (command != "exit") continue;
                    isListening = false;
                    Console.WriteLine("EXIT command received.  Stoping to listen...");
                    break;
                }
            }
        }

        private static async Task ConnectToAzureIoTHubAcync(string connectionString)
        {
            if (isConnected)
            {
                Console.WriteLine("The connection is already established for this device.");
            }
            else
            {
                if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));
                try
                {
                    Console.WriteLine("Connecting to Azure IoT Hub.  Please wait a moment...");
                    deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
                    await deviceClient.OpenAsync();

                    isConnected = true;
                    Console.WriteLine("Connection etablished to Azure IoT Hub.");
                }
                catch (Exception e)
                {
                    isConnected = false;
                    Console.WriteLine(
                        "An error occurred while connecting to Azure IoT Hub using the device connection string: " +
                        e.InnerException.Message);
                }
            }
        }
    }
}