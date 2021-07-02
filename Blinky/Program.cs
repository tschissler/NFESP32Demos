using System.Threading;
using System.Device.Gpio;

namespace NFApp1
{
    public class Program
    {
        private static GpioController GpioController;

        private static int LEDRedPin = 27;
        private static int LEDGreenPin = 14;
        private static int LEDBluePin = 16;

        public static void Main()
        {
            GpioController = new GpioController();

            var ledRed = GpioController.OpenPin(LEDRedPin, PinMode.Output);
            var ledGreen = GpioController.OpenPin(LEDGreenPin, PinMode.Output);
            var ledBlue = GpioController.OpenPin(LEDBluePin, PinMode.Output);

            ledRed.Write(PinValue.Low);
            ledGreen.Write(PinValue.Low);
            ledBlue.Write(PinValue.Low);

            while (true)
            {
                Blink(ledRed);
                Blink(ledBlue);
                Blink(ledGreen);
            }
        }

        private static void Blink(GpioPin led)
        {
            led.Toggle();
            Thread.Sleep(125);
            led.Toggle();
            Thread.Sleep(125);
            led.Toggle();
            Thread.Sleep(125);
            led.Toggle();
            Thread.Sleep(525);
        }
    }
}
