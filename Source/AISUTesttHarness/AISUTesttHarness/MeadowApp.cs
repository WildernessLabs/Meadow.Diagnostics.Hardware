using AISUTesttHarness;
using AISUTesttHarness.Models;
using Meadow;
using Meadow.Devices;
using Meadow.Foundation;
using Meadow.Foundation.Displays.Ssd130x;
using Meadow.Foundation.Displays.TftSpi;
using Meadow.Foundation.Graphics;
using Meadow.Foundation.ICs.EEPROM;
using Meadow.Foundation.Leds;
using Meadow.Foundation.Sensors.Buttons;
using Meadow.Foundation.Sensors.Temperature;
using Meadow.Hardware;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MeadowApp
{
    public class MeadowApp : App<F7FeatherV2, MeadowApp>
    {
        //==== ports and such
        RgbPwmLed onboardLed;
        AnalogTemperature[] tempSensors = new AnalogTemperature[6];

        //==== drivers
        At24Cxx at24CI2c;
        At24Cxx at24CSpi;
        IAnalogInputPort[] analogPorts = new IAnalogInputPort[6];

        //==== push buttons
        PushButton button1;
        PushButton button2;
        PushButton button3;

        //==== state
        bool i2cIsWorking = false;
        bool spiIsWorking = false;
        bool i2cAt24cIsWorking = false;
        bool spiAt24cIsWorking = false;

        //==== graphics
        MicroGraphics graphicsI2c;
        MicroGraphics graphicsSpi;

        public MeadowApp()
        {
            Initialize();

            ValidateI2cEeprom();

            UpdateSPIDisplay();

            while(true)
            {
                UpdateI2C("Reading temperature");
                UpdateSPIDisplay();
                
                Thread.Sleep(100);
            }
        }

        void JsonTest()
        {
            Product product = new Product()
            {
                Name = "Meadow",
                ExpiryDate = new DateTime(2099, 1, 20),
                Price = 34.99
            };

            string output = JsonConvert.SerializeObject(product);

            Product deserializedProduct = JsonConvert.DeserializeObject<Product>(output);
        }

        void UpdateSPIDisplay()
        {
            graphicsSpi.Clear();

            graphicsSpi.DrawRectangle(0, 0, 240, 240, Color.LawnGreen);

            for(int i = 0; i < 6; i++)
            {
               // graphicsSpi.DrawText(5, i * 20 + 5, ${ tempSensors[i].Po}
                graphicsSpi.DrawText(5, i * 20 + 5, $"Analog {i+1}: {tempSensors[i].Temperature?.Celsius:0.00}°C");
            }

            graphicsSpi.DrawText(5, 8 * 20 + 5, "I2C eeprom " + (i2cAt24cIsWorking ? "passed" : "failed"));
            graphicsSpi.DrawText(5, 9 * 20 + 5, "SPI eeprom " + (spiAt24cIsWorking ? "passed" : "unknown"));
            graphicsSpi.DrawText(5, 10 * 20 + 5, "UART unknown");

            graphicsSpi.Show();
        }

        void UpdateI2C(string message1 = "", string message2 = "")
        {
            Console.WriteLine($"{message1} {message2} {DateTime.Now}");

            if (i2cIsWorking) {

                graphicsI2c.Clear();
                graphicsI2c.DrawText(0, 0, "Meadow B6.0.1 " + DateTime.Now.ToLongTimeString());
                graphicsI2c.DrawText(0, 8, message1);
                graphicsI2c.DrawText(0, 16, message2);
                graphicsI2c.DrawText(0, 24, "Analog/I2C/SPI/UART");

                graphicsI2c.Show();
            }
        }

        void Initialize()
        {
            Console.WriteLine("Update time (async)");
            _ = UpdateClock();

            Console.WriteLine("Initialize hardware ...");
            Console.WriteLine("Intialize SSD1306");

            //---- I2C Bus
            II2cBus i2cBus = null;
            try 
            {
                i2cBus = Device.CreateI2cBus();
                i2cIsWorking = true;

            } 
            catch (Exception e) 
            {
                Console.WriteLine($" Failed to initialize I2C. Err: {e.Message}");
            }

            //---- I2C Display
            if (i2cIsWorking) 
            {
                var ssd1306 = new Ssd1306(i2cBus, 60, Ssd1306.DisplayType.OLED128x32) 
                {
                    IgnoreOutOfBoundsPixels = true
                };
                graphicsI2c = new MicroGraphics(ssd1306);
                graphicsI2c.CurrentFont = new Font6x8();

            }

            //---- onboard LED
            UpdateI2C("Init onboard RGB led");

            onboardLed = new RgbPwmLed(device: Device,
                redPwmPin: Device.Pins.OnboardLedRed,
                greenPwmPin: Device.Pins.OnboardLedGreen,
                bluePwmPin: Device.Pins.OnboardLedBlue);

            //---- buttons
            button1 = new PushButton(
                device: Device,
                inputPin: Device.Pins.D02,
                resistorMode: ResistorMode.InternalPullDown);
            button1.Clicked += (s, e) =>
            {
                Console.WriteLine("Button 1 Clicked");
            };

            button2 = new PushButton(
                device: Device,
                inputPin: Device.Pins.D06,
                resistorMode: ResistorMode.InternalPullDown);
            button2.Clicked += (s, e) =>
            {
                Console.WriteLine("Button 2 Clicked");
            };

            button3 = new PushButton(
                device: Device,
                inputPin: Device.Pins.D09,
                resistorMode: ResistorMode.InternalPullDown);
            button3.Clicked += (s, e) =>
            {
                Console.WriteLine("Button 3 Clicked");
            };

            //---- SPI Bus
            UpdateI2C("Init SPI bus");
            ISpiBus spiBus = null;
            try 
            {
                var spiConfig = new SpiClockConfiguration(new Meadow.Units.Frequency(48000, Meadow.Units.Frequency.UnitType.Kilohertz), SpiClockConfiguration.Mode.Mode3);
                spiBus = Device.CreateSpiBus(Device.Pins.SCK, Device.Pins.MOSI, Device.Pins.MISO, spiConfig);
                spiIsWorking = true;
            } 
            catch (Exception e) 
            {
                Console.WriteLine($" Failed to initialize I2C. Err: {e.Message}");
            }

            //---- SPI Display
            if (spiIsWorking) 
            {
                UpdateI2C("Init color display", "ST7789 240x240");
                var st7789 = new St7789(device: Device, spiBus: spiBus,
                    chipSelectPin: Device.Pins.D15,
                    dcPin: Device.Pins.D11,
                    resetPin: Device.Pins.D14,
                    width: 240, height: 240, 
                    displayColorMode: ColorType.Format12bppRgb444);

                graphicsSpi = new MicroGraphics(st7789);
                graphicsSpi.CurrentFont = new Font12x20();
                graphicsSpi.Rotation = RotationType._180Degrees;
            }

            //---- I2C EEPROM
            if (i2cIsWorking) 
            {
                UpdateI2C("Init I2C EEPROM");
                at24CI2c = new At24Cxx(i2cBus);
            }

            //---- SPI EEPROM
            if (spiIsWorking) 
            {
                // TODO: Driver doesn't support SPI yet.
                //UpdateI2C("Init SPI EEPROM");
                //at24CSpi = new At24Cxx()
            }

            //---- Analog Temp Sensors
            UpdateI2C("Init temp sensors");

            // pins
            var analogPins = new IPin[] 
            {
                Device.Pins.A00, 
                Device.Pins.A01, 
                Device.Pins.A02, 
                Device.Pins.A03, 
                Device.Pins.A04, 
                Device.Pins.A05
            };

            // instantiate the temp sensors
            for (int i = 0; i < 6; i++)
            {
                tempSensors[i] = new AnalogTemperature(Device, analogPins[i], AnalogTemperature.KnownSensorType.TMP235);
                tempSensors[i].StartUpdating(new TimeSpan(0, 0, 5));
            }

           // UpdateI2C("Test Json deserialize");
           // JsonTest();
        }

        async Task UpdateClock()
        {
            var result = await Device.WiFiAdapter.Connect(Secrets.WIFI_NAME, Secrets.WIFI_PASSWORD);

            var today = await ClockService.GetTime();

            MeadowApp.Device.SetClock(today.Subtract(new TimeSpan(7, 0, 0)));
        }

        bool ValidateI2cEeprom()
        {
            UpdateI2C("Write to I2C eeprom");
            var data = new byte[] { 0, 1, 2, 3, 5, 7, 11, 13, 17, 23, 29, 31, 37, 43, 53, 59 };

            at24CI2c.Write(0, data);

            UpdateI2C("Read from I2C eeprom");

            var memory = at24CI2c.Read(0, 16);

            i2cAt24cIsWorking = data.SequenceEqual(memory);

            for (ushort index = 0; index < 16; index++) {
                Thread.Sleep(50);
                Console.WriteLine("Byte: " + index + ", Value: " + memory[index]);
            }

            return i2cAt24cIsWorking;
        }
    }
}