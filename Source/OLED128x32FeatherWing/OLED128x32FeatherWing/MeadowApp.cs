using Meadow;
using Meadow.Devices;
using Meadow.Foundation;
using Meadow.Foundation.FeatherWings;
using Meadow.Foundation.Graphics;
using Meadow.Foundation.Leds;
using Meadow.Hardware;
using System;
using System.Threading;

namespace OLED128x32FeatherWing
{
    public class MeadowApp : App<F7Micro, MeadowApp>
    {
        RgbPwmLed onboardLed;

        OLED128x32Wing oledWing;

        GraphicsLibrary graphics;

        public MeadowApp()
        {
            Initialize();
        }

        void Initialize()
        {
            Console.WriteLine("Initialize hardware...");

            onboardLed = new RgbPwmLed(device: Device,
                redPwmPin: Device.Pins.OnboardLedRed,
                greenPwmPin: Device.Pins.OnboardLedGreen,
                bluePwmPin: Device.Pins.OnboardLedBlue,
                3.3f, 3.3f, 3.3f,
                Meadow.Peripherals.Leds.IRgbLed.CommonType.CommonAnode);

            var i2cBus = Device.CreateI2cBus(I2cBusSpeed.FastPlus);

            oledWing = new OLED128x32Wing(i2cBus, Device, Device.Pins.D11, Device.Pins.D10, Device.Pins.D09);

            graphics = new GraphicsLibrary(oledWing.Display);
            graphics.CurrentFont = new Font12x16();

            oledWing.ButtonA.Clicked += (sender, e) =>
            {
                Console.WriteLine("A");
                UpdateDisplay("A pressed");
            };

            oledWing.ButtonB.Clicked += (sender, e) =>
            {
                Console.WriteLine("B");
                UpdateDisplay("B pressed");
            };

            oledWing.ButtonC.Clicked += (sender, e) => {
                Console.WriteLine("C");
                UpdateDisplay("C pressed");
            };
        }

        void UpdateDisplay(string message)
        {
            graphics.Clear();
            graphics.DrawText(0, 8, message);
            graphics.Show();
        }


    }
}
