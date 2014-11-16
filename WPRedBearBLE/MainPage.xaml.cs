using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using WPRedBearBLE.Resources;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Bluetooth;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.Storage.Streams;

//tested on the Nokia Lumia 920 and the Lumia 521 both running the Windows Phone 8.1 Cyan update
//RedBear BLE Mini board was connected directly to a CP2102 USB UART and communication was tested using CoolTerm http://freeware.the-meiers.org/
//Try http://www.themethodology.net for more information

namespace WPRedBearBLE
{
    public partial class MainPage : PhoneApplicationPage
    {
        DeviceInformationCollection bleDevices;
        GattDeviceService selectedService;
        BluetoothLEDevice _device;
        IReadOnlyList<GattDeviceService> _services;
        GattDeviceService _service;
        GattCharacteristic _characteristic;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Sample code to localize the ApplicationBar

            RedBearConnect();
        }

        async Task RedBearConnect ()
        {
            if (await RefreshDeviceList() == 0)
                return;

            if(await ConnectToRedBear() == false)
                return;

            if (await FindService() == false)
                return;

            if (await FindCharacteristic() == false)
                return;

            //send a message        
            SendMessage("Hello RedBear!");
        
        }

        async Task<int> RefreshDeviceList()
        {
            try
            {
                bleDevices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromUuid(GattServiceUuids.GenericAccess));

                OutputMessage("Found " + bleDevices.Count + " devices");
             
                if (bleDevices.Count == 0)
                {
                    OutputMessage("No BLE Devices found - make sure you've paired your device");
                    Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-bluetooth:", UriKind.RelativeOrAbsolute));
                }
            }
            catch (Exception ex)
            {
                OutputMessage("Failed to find BLE devices: " + ex.Message);
            }

            return bleDevices.Count;
        }

        async Task<bool> ConnectToRedBear ()
        {
            try
            {
                for (int i = 0; i < bleDevices.Count; i++ )
                {
                    if(bleDevices[i].Name == "Biscuit")
                    {
                        _device = await BluetoothLEDevice.FromIdAsync(bleDevices[i].Id);
                        _services = _device.GattServices;
                        OutputMessage("Found Device: " + _device.Name);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                OutputMessage("Connection failed: " + ex.Message);
                return false;
            }

            OutputMessage("Unable to find device Biscuit - has it been paired?");

            return true;
        }

        async Task<bool> FindService ()
        {
            foreach (GattDeviceService s in _services)
            {
                if (s.Uuid == new Guid("713d0000-503e-4c75-ba94-3148f18d941e"))
                {
                    _service = s;
                    OutputMessage("Found Service: " + s.Uuid);
                    return true;
                }
                
            }
            OutputMessage("Unable to find Biscuit Service 713d0000");
            return false;
        }

        async Task<bool> FindCharacteristic ()
        {
            foreach (var c in _service.GetCharacteristics(new Guid("713d0003-503e-4c75-ba94-3148f18d941e")))
            {
                //"unauthorized access" without proper permissions
                _characteristic = c;
                OutputMessage("Found characteristic: " + c.Uuid);
                return true;
            }

            OutputMessage("Could not find characteristic or permissions are incorrrect");
            return false;
        }

        async Task<bool> SendMessage (string msg)
        {
            DataWriter data = new DataWriter();
            data.WriteString(msg);

            var buffer = data.DetachBuffer();

            try 
            {
                //first chance exception
                await _characteristic.WriteValueAsync(buffer, GattWriteOption.WriteWithoutResponse);

                OutputMessage("Sent message: " + msg);
            }
            catch (Exception ex)
            {
                OutputMessage("Unable to send message: " + ex.Message);
                return false;
            }
            return true;

            
        }

        void OutputMessage (string msg)
        {
            Debug.WriteLine(msg);
            txtOutput.Text += msg + "\r\n";
        }

    }
}