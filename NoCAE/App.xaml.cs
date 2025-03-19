using System.Windows;

namespace NoCAE
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // Below are the clientId (Application Id) of your app registration and the tenant information. 
        // You have to replace:
        // - the content of ClientID with the Application Id for your app registration

        // Static field to store the Client ID for the application registration.
        // This ID is used to identify the application in the authentication process.
        public static string ClientId = "bd83ff0f-bc47-4c56-9d0c-78de2b5eb63c";

        // Constructor for the App class
        public App()
        {
            // Log the initialization of the application
            Console.WriteLine("Application is starting.");

            // Additional initialization code can be added here if necessary

            // Log the completion of the application initialization
            Console.WriteLine("Application has started.");
        }
    }
}