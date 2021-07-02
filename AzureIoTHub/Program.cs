//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using nanoFramework.Azure.Devices.Client;
using nanoFramework.Azure.Devices.Shared;
using nanoFramework.Networking;
using System;
using System.Collections;
using System.Threading;
using System.Diagnostics;
using Windows.Devices.Pwm;
using nanoFramework.Hardware.Esp32;
using nanoFramework.Json;


namespace AzureIoTHub
{
    public class Program
    {
        const string DeviceID = "NFDemo";
        const string IotBrokerAddress = "IoTProdM1Hub.azure-devices.net";
        const string SasKey = "BzNVym01Jr3rqtY97v1961iexCQHK0Fc9MdmVvCKusk=";
        const string Ssid = "agileMax_Guest";
        const string Password = "WLAN_agileMax";

        private static int LEDRedPin = 27;
        private static int LEDGreenPin = 14;
        private static int LEDBluePin = 16;

        private static PwmPin pwmPinRed;
        private static PwmPin pwmPinGreen;
        private static PwmPin pwmPinBlue;

        // One minute unit
        static int sleepTimeMinutes = 60000;

        // If you don't have a BMP280, comment this part:
        //const int busId = 1;
        //I2cConnectionSettings i2cSettings = new(busId, Bmp280.DefaultI2cAddress);
        //I2cDevice i2cDevice = I2cDevice.Create(i2cSettings);
        //Bmp280 bmp280 = new Bmp280(i2cDevice);
        // Up to here!

        static bool ShoudIStop = false;



        public static void Main()
        {
            Configuration.SetPinFunction(LEDRedPin, DeviceFunction.PWM1);
            Configuration.SetPinFunction(LEDGreenPin, DeviceFunction.PWM1);
            Configuration.SetPinFunction(LEDBluePin, DeviceFunction.PWM1);

            var pwmController = PwmController.FromId("PWM1");
            pwmController.SetDesiredFrequency(50000);

            pwmPinRed = pwmController.OpenPin(LEDRedPin);
            pwmPinRed.SetActiveDutyCyclePercentage(0);
            pwmPinRed.Start();

            pwmPinGreen = pwmController.OpenPin(LEDGreenPin);
            pwmPinGreen.SetActiveDutyCyclePercentage(0);
            pwmPinGreen.Start();

            pwmPinBlue = pwmController.OpenPin(LEDBluePin);
            pwmPinBlue.SetActiveDutyCyclePercentage(0);
            pwmPinBlue.Start();

            DeviceClient azureIoT = new DeviceClient(IotBrokerAddress, DeviceID, SasKey, 0);
            try
            {
                if (!ConnectToWifi()) return;

                azureIoT.TwinUpated += TwinUpdatedEvent;
                azureIoT.StatusUpdated += StatusUpdatedEvent;
                azureIoT.CloudToDeviceMessage += CloudToDeviceMessageEvent;
                azureIoT.AddMethodCallback(MethodCalbackTest);
                azureIoT.AddMethodCallback(MakeAddition);
                azureIoT.AddMethodCallback(RaiseExceptionCallbackTest);
                var isOpen = azureIoT.Open();
                Debug.WriteLine($"Connection is open: {isOpen}");

                var twin = azureIoT.GetTwin(new CancellationTokenSource(20000).Token);
                if (twin == null)
                {
                    Debug.WriteLine($"Can't get the twins");
                    azureIoT.Close();
                    return;
                }

                Debug.WriteLine(
                    $"Twin DeviceID: {twin.DeviceId}, #desired: {twin.Properties.Desired.Count}, #reported: {twin.Properties.Reported.Count}");

                TwinCollection reported = new TwinCollection();
                reported.Add("firmware", "myNano");
                reported.Add("sdk", 0.2);
                azureIoT.UpdateReportedProperties(reported);

                UpdateLEDColorFromTwin(twin.Properties.Desired);
                // set higher sampling
                //bmp280.TemperatureSampling = Sampling.LowPower;
                //bmp280.PressureSampling = Sampling.UltraHighResolution;

                while (!ShoudIStop)
                {
                    // If you don't have a BMP280, comment this part:
                    //var values = bmp280.Read();
                    //azureIoT.SendMessage($"{{\"Temperature\":{values.Temperature.DegreesCelsius},\"Pressure\":{values.Pressure.Hectopascals}}}");
                    // Up to here!
                    // And uncomment the following line:
                    // azureIoT.SendMessage($"{{\"Temperature\":42,\"Pressure\":1023}}");
                    Thread.Sleep(20000);
                }

            }
            catch (Exception ex)
            {
                // We won't do anything
                // This global try catch is to make sure whatever happen, we will safely be able to
                // reboot or do anything else.
                Debug.WriteLine(ex.ToString());
            }

            Thread.Sleep(Timeout.InfiniteTimeSpan);
        }

        static bool ConnectToWifi()
        {
            Debug.WriteLine("Program Started, connecting to WiFi.");

            // As we are using TLS, we need a valid date & time
            // We will wait maximum 1 minute to get connected and have a valid date
            var success = NetworkHelper.ConnectWifiDhcp(Ssid, Password, setDateTime: true,
                token: new CancellationTokenSource(sleepTimeMinutes).Token);
            if (!success)
            {
                Debug.WriteLine($"Can't connect to wifi: {NetworkHelper.ConnectionError.Error}");
                if (NetworkHelper.ConnectionError.Exception != null)
                {
                    Debug.WriteLine($"NetworkHelper.ConnectionError.Exception");
                }
            }

            Debug.WriteLine($"Date and time is now {DateTime.UtcNow}");
            return success;
        }

        static void UpdateLEDColorFromTwin(TwinCollection twin)
        {
            if (twin.Contains("Red"))
            {
                pwmPinRed.SetActiveDutyCyclePercentage((double)Int16.Parse(twin["Red"].ToString()) / 100);
            }
            if (twin.Contains("Green"))
            {
                pwmPinGreen.SetActiveDutyCyclePercentage((double)Int16.Parse(twin["Green"].ToString()) / 100);
            }
            if (twin.Contains("Blue"))
            {
                pwmPinBlue.SetActiveDutyCyclePercentage((double)Int16.Parse(twin["Blue"].ToString()) / 100);
            }

        }

        static void TwinUpdatedEvent(object sender, TwinUpdateEventArgs e)
        {
            Debug.WriteLine($"Twin update received:  {e.Twin.Count}");

            UpdateLEDColorFromTwin(e.Twin);
        }

        static void StatusUpdatedEvent(object sender, StatusUpdatedEventArgs e)
        {
            Debug.WriteLine($"Status changed: {e.IoTHubStatus.Status}, {e.IoTHubStatus.Message}");
            // You may want to reconnect or use a similar retry mechanism
            if (e.IoTHubStatus.Status == Status.Disconnected)
            {
                Debug.WriteLine("Stoppped!!!");
            }
        }

        static string MethodCalbackTest(int rid, string payload)
        {
            Debug.WriteLine($"Call back called :-) rid={rid}, payload={payload}");
            return "{\"Yes\":\"baby\",\"itisworking\":42}";
        }

        static string MakeAddition(int rid, string payload)
        {
            Hashtable variables = (Hashtable)JsonConvert.DeserializeObject(payload, typeof(Hashtable));
            int arg1 = (int)variables["arg1"];
            int arg2 = (int)variables["arg2"];
            return $"{{\"result\":{arg1 + arg2}}}";
        }

        static string RaiseExceptionCallbackTest(int rid, string payload)
        {
            throw new Exception("I got you, it's to test the 504");
        }

        static void CloudToDeviceMessageEvent(object sender, CloudToDeviceMessageEventArgs e)
        {
            Debug.WriteLine($"Message arrived: {e.Message}");
            foreach (string key in e.Properties.Keys)
            {
                Debug.Write($"  Key: {key} = ");
                if (e.Properties[key] == null)
                {
                    Debug.WriteLine("null");
                }
                else
                {
                    Debug.WriteLine((string)e.Properties[key]);
                }
            }

            if (e.Message == "stop")
            {
                ShoudIStop = true;
            }
        }
    }
}