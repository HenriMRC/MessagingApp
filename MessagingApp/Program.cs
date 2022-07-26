namespace MessagingApp
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            bool isServer = false;
            
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "server":
                        isServer = true;
                        break;
                }
            }

            NetworkBase network = isServer ? new Server() : new Client();

            MainWindow mainWindow = new MainWindow(network);

            if (isServer)
            {
                /*ConnectionWindow connectionWindow = new ConnectionWindow(mainWindow.Close, isServer);
                connectionWindow.Show(mainWindow);*/
            }

            Application.Run(mainWindow);
        }
    }
}