
using Microsoft.Win32.TaskScheduler;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Windows.Devices.Geolocation;
using Windows.UI.Xaml.Navigation;

namespace BackgroundService
{
    public class GeoLocation
    {
        public static void WinServiceStart()
        {
            string serviceName = "lfsvc";

            try
            {
                // Create a ServiceController for the Geolocation Service
                ServiceController serviceController = new ServiceController(serviceName);

                // Check if the service is currently stopped
                if (serviceController.Status == ServiceControllerStatus.Stopped)
                {
                    // Start the service
                    serviceController.Start();
                    serviceController.WaitForStatus(ServiceControllerStatus.Running);

                    Console.WriteLine("Location service started successfully.");
                }
                else
                {
                    Console.WriteLine("Location service is already running.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        public static void WinServiceStop()
        {
            string serviceName = "lfsvc";

            try
            {
                // Create a ServiceController for the Geolocation Service
                ServiceController serviceController = new ServiceController(serviceName);

                // Check if the service is currently running
                if (serviceController.Status == ServiceControllerStatus.Running)
                {
                    // Stop the service
                    serviceController.Stop();
                    serviceController.WaitForStatus(ServiceControllerStatus.Stopped);

                    Console.WriteLine("Location service stopped successfully.");
                }
                else
                {
                    Console.WriteLine("Location service is already stopped.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        //windows current lat long location get using c#
        public static async void GetLocation()
        {

        }
    }
}

