using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using WPRedBearBLE2.Resources;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Bluetooth;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.Storage.Streams;

//tested on the Nokia Lumia 920 and the Lumia 521 both running the Windows Phone 8.1 Cyan update
//RedBear BLE Mini board was connected directly to a CP2102 USB UART and communication was tested using CoolTerm http://freeware.the-meiers.org/
//Try http://www.themethodology.net for more information

namespace WPRedBearBLE2
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

            RedBearConnect();

            btnSendMessage.Click += btnSendMessage_Click;
        }

        void btnSendMessage_Click(object sender, RoutedEventArgs e)
        {
            SendMessage(txtMessage.Text);
            txtMessage.Text = String.Empty;
        }

        async Task RedBearConnect()
        {
            if (await RefreshDeviceList() == 0)
                return;

            if (await ConnectToRedBear() == false)
                return;

            if (FindService() == false)
                return;

            if (FindCharacteristic() == false)
                return;

            txtMessage.IsEnabled = true;
            btnSendMessage.IsEnabled = true;
        }

        async Task<int> RefreshDeviceList()
        {
            try
            {
                bleDevices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromUuid(GattServiceUuids.GenericAccess));

                OutputMessage("Found " + bleDevices.Count + " device(s)");

                if (bleDevices.Count == 0)
                {
                    OutputMessage("No BLE Devices found - make sure you've paired your device");
                    await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-bluetooth:", UriKind.RelativeOrAbsolute));
                }
            }
            catch (Exception ex)
            {
                OutputMessage("Failed to find BLE devices: " + ex.Message);
            }

            return bleDevices.Count;
        }

        async Task<bool> ConnectToRedBear()
        {
            try
            {
                for (int i = 0; i < bleDevices.Count; i++)
                {
                    if (bleDevices[i].Name == "Biscuit" || bleDevices[i].Name == "BLE Mini")
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

        bool FindService()
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

        bool FindCharacteristic()
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

        async Task<bool> SendMessage(string msg)
        {
            DataWriter data = new DataWriter();
            data.WriteString(msg);

            var buffer = data.DetachBuffer();

            try
            {
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

        void OutputMessage(string msg)
        {
            Debug.WriteLine(msg);
            txtOutput.Text += msg + "\r\n";
        }

    }
}