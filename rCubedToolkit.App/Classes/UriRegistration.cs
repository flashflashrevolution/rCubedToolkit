using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace rCubedToolkit
{
    internal static class UriRegistration
    {
        internal static string URI_KEY_ROOT = @"Software\Classes\rCubed";
        internal static string URI_KEY_COMMAND = @"shell\open\command";
        internal static string URI_KEY_ICON = @"DefaultIcon";

        public static void RegisterInBackground()
        {
            _ = Register();
            Application.Current.Shutdown();
        }

        public static void UnregisterInBackground()
        {
            _ = Unregister();
            Application.Current.Shutdown();
        }

        public static bool IsRegistered()
        {
            bool isRegistered = false;
            if (Registry.CurrentUser.OpenSubKey(URI_KEY_ROOT) is var protocolKey && protocolKey != null)
            {
                if (protocolKey.OpenSubKey(URI_KEY_COMMAND) is var commandKey)
                {
                    isRegistered = commandKey.GetValue("").ToString().Equals($"\"{Path.Combine(App.InstallFolder, App.InstallExe)}\" \"%1\"");
                }
            }

            return isRegistered;
        }

        public static bool Register()
        {
            bool successfullyRegistered = false;

            try
            {
                if (!IsRegistered())
                {
                    string fullInstallPath = Path.Combine(App.InstallFolder, App.InstallExe);

                    // Create root of URI registration tree.
                    RegistryKey protocolKey = Registry.CurrentUser.CreateSubKey(URI_KEY_ROOT, true);
                    protocolKey.SetValue("", "URL:ffr protocol");
                    protocolKey.SetValue("URL Protocol", "");

                    // Create icon entry for URI registration.
                    RegistryKey iconKey = protocolKey.CreateSubKey(URI_KEY_ICON, true);
                    iconKey.SetValue("", $"\"{fullInstallPath}\"");

                    // Create command entry for URI registration.
                    RegistryKey commandKey = protocolKey.CreateSubKey(URI_KEY_COMMAND, true);
                    commandKey.SetValue("", $"\"{fullInstallPath}\" \"%1\"");

                    successfullyRegistered = true;
                }
            }
            catch (Exception e)
            {
                _ = MessageBox.Show(e.ToString());
            }

            return successfullyRegistered;
        }

        public static bool Unregister()
        {
            bool successfullyUnregistered = false;

            if (IsRegistered())
            {
                try
                {
                    if (Registry.CurrentUser.OpenSubKey(URI_KEY_ROOT, true) is var protocolKey && protocolKey != null)
                    {
                        Registry.CurrentUser.DeleteSubKeyTree(URI_KEY_ROOT);
                        successfullyUnregistered = true;
                    }
                }
                catch (Exception e)
                {
                    _ = MessageBox.Show(e.ToString());
                }
            }

            return successfullyUnregistered;
        }
    }
}
