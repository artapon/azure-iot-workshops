using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common.Exceptions;

namespace AzureDeviceIdentityManager
{
    internal class Program
    {
        private const int MAX_DEVICE_COUNT = 10000;

        private static RegistryManager registryManager;
        private static string connectionString;

        private static void Main(string[] args)
        {
            var userInput = -1;
            Console.Clear();
            Console.Write("Enter a connection string to Azure Iot Hub: ");
            connectionString = Console.ReadLine();
            InitilizeRegistryManagerAsync().Wait();
            ListRegisteredDevicesAsync().Wait();

            do
            {
                string deviceId;
                switch (UserPrompt())
                {
                    case 0:
                        userInput = 0;
                        break;
                    case 1:
                        Console.Write("Enter a device ID to register: ");
                        deviceId = Console.ReadLine();
                        CreateDeviceAync(deviceId).Wait();
                        break;
                    case 2:
                        Console.Write("Enter the device ID to be removed: ");
                        deviceId = Console.ReadLine();
                        DeleteDeviceAync(deviceId).Wait();
                        break;
                    case 3:
                        Console.Write("Listing devices in the registry... ");
                        ListRegisteredDevicesAsync().Wait();
                        break;
                    default:
                        userInput = -1;
                        break;
                }
            } while (userInput != 0);

            DisconnectFromHub().Wait();
            Console.WriteLine("Disconnected from Azure IoT Hub.  Enter a key to exit.");
            Console.ReadKey();
        }

        private static async Task DisconnectFromHub()
        {
            if (registryManager != null)
            {
                await registryManager.CloseAsync();
            }
        }
        private static int UserPrompt()
        {
            var userInput = 0;
            Console.WriteLine(" '1' to add a device");
            Console.WriteLine(" '2' to delete a device");
            Console.WriteLine(" '3' to list devices");
            Console.WriteLine(" '0' to exit");
            return int.TryParse(Console.ReadLine(), out userInput) ? userInput : -1;
        }

        private static async Task DeleteDeviceAync(string deviceId)
        {
            if (registryManager != null)
            {
                try
                {
                    await registryManager.RemoveDeviceAsync(deviceId);
                    Console.WriteLine($"A device {deviceId} has been succesfully removed from the registry.");
                    Console.WriteLine();
                    ListRegisteredDevicesAsync().Wait();
                }
                catch (DeviceNotFoundException e)
                {
                    Console.WriteLine($"The device: {deviceId} is not found in the registry: " + e.Message);
                }
            }
        }
        private static async Task CreateDeviceAync(string deviceId)
        { 
            var device = new Device(deviceId);
            if (registryManager != null)
            {
                try
                {
                    device = await registryManager.AddDeviceAsync(device);
                    Console.WriteLine($"A device {device.Id} succesfully added to the registry");
                    Console.WriteLine($"Primary Key: {device.Authentication.SymmetricKey.PrimaryKey}");
                    Console.WriteLine($"Secondary Key: {device.Authentication.SymmetricKey.SecondaryKey}");
                    Console.WriteLine();
                }
                catch (DeviceAlreadyExistsException e)
                {
                    Console.WriteLine($"The device: {deviceId} already exists in the registry.  Please try with another name.");
                    Console.WriteLine();
                }
            }
        }

        private static async Task ListRegisteredDevicesAsync()
        {
            if (registryManager != null)
            {
                var devices = new List<Device>(await registryManager.GetDevicesAsync(MAX_DEVICE_COUNT));
                Console.WriteLine($"{devices.Count} device(s) found in the registry.");
                Console.WriteLine();

                if (devices.Count > 0)
                {

                    foreach (var device in devices)
                    {
                        Console.WriteLine($"device ID: {device.Id}");
                        Console.WriteLine($"device Status: {device.ConnectionState}");
                        Console.WriteLine($"device Key: {device.Authentication.SymmetricKey.PrimaryKey}");
                    }

                    Console.WriteLine();
                }
            }
        }


        private static async Task InitilizeRegistryManagerAsync()
        {
            try
            {
                registryManager = RegistryManager.CreateFromConnectionString(connectionString);
                await registryManager.OpenAsync();
            }
            catch (Exception e)
            {
                Debug.WriteLine("An error occurred while connecting to Azure IoT Hub.  Ensure that your connection string is valid: " + e.Message);
                Console.WriteLine();
            }
        }
    }
}