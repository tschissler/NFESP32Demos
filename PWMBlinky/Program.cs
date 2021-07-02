using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.Threading;
using Windows.Devices.Pwm;
using nanoFramework.Hardware.Esp32;

namespace PWMBlinky
{
    public class Program
    {
        private static int LEDRedPin = 27;
        private static int LEDGreenPin = 14;
        private static int LEDBluePin = 16;

        public static void Main()
        {
            Configuration.SetPinFunction(LEDRedPin, DeviceFunction.PWM1);
            Configuration.SetPinFunction(LEDGreenPin, DeviceFunction.PWM1);
            Configuration.SetPinFunction(LEDBluePin, DeviceFunction.PWM1);

            var pwmController = PwmController.FromId("PWM1");
            pwmController.SetDesiredFrequency(5000);

            var pwmPinRed = pwmController.OpenPin(LEDRedPin);
            pwmPinRed.SetActiveDutyCyclePercentage(0);
            pwmPinRed.Start();

            var pwmPinGreen = pwmController.OpenPin(LEDGreenPin);
            pwmPinGreen.SetActiveDutyCyclePercentage(0);
            pwmPinGreen.Start();

            var pwmPinBlue = pwmController.OpenPin(LEDBluePin);
            pwmPinBlue.SetActiveDutyCyclePercentage(0);
            pwmPinBlue.Start();

            while (true)
            {
                DutyCycle(pwmPinRed);
                DutyCycle(pwmPinGreen);
                DutyCycle(pwmPinBlue);
            }
        }

        private static void DutyCycle(PwmPin pwmPin)
        {
            for (int dutyCycle = 0; dutyCycle <= 100; dutyCycle += 5)
            {
                pwmPin.SetActiveDutyCyclePercentage((double)dutyCycle/100);
                Thread.Sleep(50);
            }

            for (int dutyCycle = 100; dutyCycle >= 0; dutyCycle -= 5)
            {
                pwmPin.SetActiveDutyCyclePercentage((double)dutyCycle/100);
                Thread.Sleep(50);
            }
        }
    }
}
