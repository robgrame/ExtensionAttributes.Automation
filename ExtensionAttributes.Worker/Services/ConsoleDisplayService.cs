using System.Reflection;

namespace ExtensionAttributes.Automation.WorkerSvc.Services
{
    public class ConsoleDisplayService
    {
        public void ShowApplicationHeader()
        {
            // Set the default console color to green
            Console.ForegroundColor = ConsoleColor.Green;
            Console.BackgroundColor = ConsoleColor.Black;

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("------------------------------------------------------------------------------------------------------");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Extension Attributes Automation Worker Service");
            
            var version = GetApplicationVersion();
            Console.WriteLine($"Version: {version}");
            Console.WriteLine("Copyright (c) 2025 Extension Attributes Automation Community");
            Console.WriteLine("All rights reserved.");
            Console.WriteLine("This program is licensed under GPL 3.0");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("------------------------------------------------------------------------------------------------------");
        }

        public void ShowLogo()
        {
            try
            {
                var exePath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? string.Empty)!;
                var logoPath = Path.Combine(exePath, "logo.txt");
                
                if (!File.Exists(logoPath))
                {
                    return;
                }

                string[] logoLines = File.ReadAllLines(logoPath);

                // Colorize the logo based on letter. A, C, I will be blue, rest will be dark gray. If space, colorize with white
                for (int i = 0; i < logoLines.Length; i++)
                {
                    for (int j = 0; j < logoLines[i].Length; j++)
                    {
                        Console.ForegroundColor = logoLines[i][j] switch
                        {
                            'A' or 'C' or 'I' => ConsoleColor.Blue,
                            ' ' => ConsoleColor.White,
                            _ => ConsoleColor.DarkGray
                        };
                        Console.Write(logoLines[i][j]);
                    }
                    Console.WriteLine();
                }

                Console.ResetColor();
            }
            catch (Exception ex)
            {
                // Log but don't fail if logo can't be displayed
                Console.WriteLine($"Could not display logo: {ex.Message}");
            }
        }

        private static string GetApplicationVersion()
        {
            try
            {
                var assemblyPath = Assembly.GetExecutingAssembly()?.Location ?? string.Empty;
                return System.Diagnostics.FileVersionInfo.GetVersionInfo(assemblyPath).FileVersion?.ToString() ?? "Unknown version";
            }
            catch
            {
                return "Unknown version";
            }
        }
    }
}