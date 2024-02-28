using Meadow;
using Meadow.Devices;
using Meadow.Foundation.Displays;
using Meadow.Foundation.Graphics;
using Meadow.Foundation.Leds;
using Meadow.Peripherals.Displays;
using Meadow.Peripherals.Leds;
using System;
using System.Threading.Tasks;

namespace PowerTest
{
    // Change F7FeatherV2 to F7FeatherV1 for V1.x boards
    public class MeadowApp : App<F7FeatherV1>
    {
        RgbPwmLed onboardLed;

        MicroGraphics graphics;

        DateTime bootTime;

        TimeSpan sleepTime = TimeSpan.FromSeconds(30); //new TimeSpan(18, 1, 0);

        int cycles = 0;

        public override Task Initialize()
        {
            Resolver.Log.Info("Initialize...");

            onboardLed = new RgbPwmLed(
                redPwmPin: Device.Pins.OnboardLedRed,
                greenPwmPin: Device.Pins.OnboardLedGreen,
                bluePwmPin: Device.Pins.OnboardLedBlue,
            CommonType.CommonAnode);

            var display = new Il91874(Device.CreateSpiBus(), Device.Pins.A04, Device.Pins.A03, Device.Pins.A02, Device.Pins.A01);

            graphics = new MicroGraphics(display);
            graphics.CurrentFont = new Font12x16();
            graphics.Rotation = RotationType._270Degrees;

          //  Device.PlatformOS.SetClock(new DateTime(2023, 6, 23, 16, 10, 0));
            bootTime = DateTime.Now;

            return base.Initialize();
        }

        public override async Task Run()
        {
            Resolver.Log.Info("Run...");

            while (true)
            {
                cycles++;

                graphics.Clear();

                graphics.DrawText(5, 5, $"Sleep Test:", scaleFactor: ScaleFactor.X1, color: Color.Black);
                graphics.DrawText(5, 25, $"{sleepTime}", scaleFactor: ScaleFactor.X1, color: Color.Black);

                graphics.DrawText(5, 55, $"Cycle: {cycles}", scaleFactor: ScaleFactor.X1, color: Color.Black);
                graphics.DrawText(5, 85, $"Start time:", scaleFactor: ScaleFactor.X1, color: Color.Black);
                graphics.DrawText(5, 105, $"{bootTime}", scaleFactor: ScaleFactor.X1, color: Color.Black);

                graphics.DrawText(5, 135, $"Last update:", scaleFactor: ScaleFactor.X1, color: Color.Black);
                graphics.DrawText(5, 155, $"{DateTime.Now}", scaleFactor: ScaleFactor.X1, color: Color.Black);

                graphics.Show();

                Device.PlatformOS.Sleep(sleepTime);

                sleepTime = InceaseSleepTime(sleepTime);
            }
        }

        TimeSpan InceaseSleepTime(TimeSpan timeSpan)
        {
            return timeSpan + TimeSpan.FromSeconds(5);
        }
    }
}