using System.Windows;

namespace NoCAE
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static App()
        {
            // Log the initialization of the App class
            Console.WriteLine("App class static constructor called.");
        }

        // Below are the clientId (Application Id) of your app registration and the tenant information. 
        // You have to replace:
        // - the content of ClientID with the Application Id for your app registration
        public static string ClientId = "bd83ff0f-bc47-4c56-9d0c-78de2b5eb63c";

        // Log the current ClientId value for debugging purposes
        static App()
        {
            Console.WriteLine($"Current ClientId: {ClientId}");
        }
    }
}