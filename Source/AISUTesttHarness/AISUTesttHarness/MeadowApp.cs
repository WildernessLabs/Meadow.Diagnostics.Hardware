using System;
using System.Linq;
using System.Threading;
using Meadow;
using Meadow.Devices;
using Meadow.Foundation;
using Meadow.Foundation.Displays.Ssd130x;
using Meadow.Foundation.Displays.TftSpi;
using Meadow.Foundation.Graphics;
using Meadow.Foundation.ICs.EEPROM;
using Meadow.Foundation.Leds;
using Meadow.Foundation.Sensors.Temperature;
using Meadow.Hardware;
using static Meadow.Foundation.Displays.DisplayBase;

namespace MeadowApp
{
    public class MeadowApp : App<F7Micro, MeadowApp>
    {
        RgbPwmLed onboardLed;
        GraphicsLibrary graphicsI2c;
        GraphicsLibrary graphicsSpi;

        At24Cxx at24CI2c;
        At24Cxx at24CSpi;

        bool isAt24CI2cWorking = false;
        bool isAt24CSpiWorking = false;

        AnalogTemperature[] tempSensors = new AnalogTemperature[8];

        public MeadowApp()
        {
            Initialize();

            UpdateSPIDisplay();

            while(true)
            {
                UpdateI2C("Reading temperature");
                UpdateSPIDisplay();
                
                Thread.Sleep(100);
            }
        }

        void UpdateSPIDisplay()
        {
            graphicsSpi.Clear();

            graphicsSpi.DrawRectangle(0, 0, 240, 240, Color.LawnGreen);

            for(int i = 0; i < 6; i++)
            {
                graphicsSpi.DrawText(5, i * 20 + 5, $"Analog {i+1}: {tempSensors[i].Temperature?.Celsius:0.00}°C");
            }

            graphicsSpi.DrawText(5, 8 * 20 + 5, "I2C eeprom " + (isAt24CI2cWorking ? "passed" : "failed"));
            graphicsSpi.DrawText(5, 9 * 20 + 5, "SPI eeprom " + (isAt24CSpiWorking ? "passed" : "unknown"));
            graphicsSpi.DrawText(5, 10 * 20 + 5, "UART unknown");

            graphicsSpi.Show();
        }

        void UpdateI2C(string message1 = "", string message2 = "")
        {
            Console.WriteLine($"{message1} {message2}");

            graphicsI2c.Clear();
            graphicsI2c.DrawText(0, 0, "Meadow B5.0.2 " + DateTime.Now.ToLongTimeString());
            graphicsI2c.DrawText(0, 8, message1);
            graphicsI2c.DrawText(0, 16, message2);
            graphicsI2c.DrawText(0, 24, "Analog/I2C/SPI/UART v1.c");

            graphicsI2c.Show();
        }

        void Initialize()
        {
            Console.WriteLine("Initialize hardware ...");
            Console.WriteLine("Intialize SSD1306");
            var i2cBus = Device.CreateI2cBus();
            var ssd1306 = new Ssd1306(i2cBus, 60, Ssd1306.DisplayType.OLED128x32);
            graphicsI2c = new GraphicsLibrary(ssd1306);
            graphicsI2c.CurrentFont = new Font4x8();

            UpdateI2C("Init onboard RGB led");

            onboardLed = new RgbPwmLed(device: Device,
                redPwmPin: Device.Pins.OnboardLedRed,
                greenPwmPin: Device.Pins.OnboardLedGreen,
                bluePwmPin: Device.Pins.OnboardLedBlue,
                3.3f, 3.3f, 3.3f,
                Meadow.Peripherals.Leds.IRgbLed.CommonType.CommonAnode);

            onboardLed.StartPulse(Color.Purple, 3000);

            UpdateI2C("Init SPI bus");

            var config = new SpiClockConfiguration(24000, SpiClockConfiguration.Mode.Mode3);
            var spiBus = Device.CreateSpiBus(Device.Pins.SCK, Device.Pins.MOSI, Device.Pins.MISO, config);

            UpdateI2C("Init color display", "ST7789 240x240");
            var st7789 = new St7789(device: Device, spiBus: spiBus,
                chipSelectPin: Device.Pins.D15,
                dcPin: Device.Pins.D11,
                resetPin: Device.Pins.D14,
                width: 240, height: 240, displayColorMode: DisplayColorMode.Format12bppRgb444);

            graphicsSpi = new GraphicsLibrary(st7789);
            graphicsSpi.CurrentFont = new Font12x20();
            graphicsSpi.Rotation = GraphicsLibrary.RotationType._180Degrees;

            UpdateI2C("Init I2C EEPROM");
            at24CI2c = new At24Cxx(i2cBus);

            UpdateI2C("Write to I2C eeprom");
            var data = new byte[] { 0, 1, 2, 3, 5, 7, 11, 13, 17, 23, 29, 31, 37, 43, 53, 59 };

            at24CI2c.Write(0, data);

            UpdateI2C("Read from I2C eeprom");

            var memory = at24CI2c.Read(0, 16);

            isAt24CI2cWorking = data.SequenceEqual(memory);

            for (ushort index = 0; index < 16; index++)
            {
                Thread.Sleep(50);
                Console.WriteLine("Byte: " + index + ", Value: " + memory[index]);
            } 

            //reset is D14
            //CD is D15
            //DC 11

            UpdateI2C("Init temp sensors");
            tempSensors[0] = new AnalogTemperature(Device.CreateAnalogInputPort(Device.Pins.A00), AnalogTemperature.KnownSensorType.LM35);
            tempSensors[1] = new AnalogTemperature(Device.CreateAnalogInputPort(Device.Pins.A01), AnalogTemperature.KnownSensorType.LM35);
            tempSensors[2] = new AnalogTemperature(Device.CreateAnalogInputPort(Device.Pins.A02), AnalogTemperature.KnownSensorType.LM35);
            tempSensors[3] = new AnalogTemperature(Device.CreateAnalogInputPort(Device.Pins.A03), AnalogTemperature.KnownSensorType.LM35);
            tempSensors[4] = new AnalogTemperature(Device.CreateAnalogInputPort(Device.Pins.A04), AnalogTemperature.KnownSensorType.LM35);
            tempSensors[5] = new AnalogTemperature(Device.CreateAnalogInputPort(Device.Pins.A05), AnalogTemperature.KnownSensorType.LM35);

            tempSensors[0].StartUpdating();
            tempSensors[1].StartUpdating();
            tempSensors[2].StartUpdating();
            tempSensors[3].StartUpdating();
            tempSensors[4].StartUpdating();
            tempSensors[5].StartUpdating();
        }
    }
}