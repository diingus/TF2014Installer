using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using SteamKit2;
using System.ComponentModel;
using System.Net;
using System.IO.Compression;
using Ionic.Zip;

namespace DepotDownloader
{
    class Program
    {
        static void Main()
        {
            Console.Title = "TF2014 Installer";

            Console.Out.WriteLine("Unfortunately, since TF2 has a Free To Play liscense, you will need to log into a Steam account to download the game");
            Console.Out.WriteLine("Also, as an attempt to keep you logged into Steam and because of certain security precautions, you will need to enter your Steam Guard code each time (3) the script accesses Steam servers");
            Console.Out.Write("Username: ");
            string username = Console.In.ReadLine();
            Console.Out.Write("Passoword: ");
            string password = ReadPassword();

            string[] download440 = new string[8] { "-username", username, "-password", password, "-app", "440", "-depot", "440" };

            string[] download232251 = new string[10] { "-username", username, "-password", password, "-app", "440", "-depot", "232251", "-manifest", "2879089989606018346" };

            string[] download441 = new string[10] { "-username", username, "-password", password, "-app", "440", "-depot", "441", "-manifest", "7174057601743547991" };

            Download(download440);
            Download(download232251);
            Download(download441);
            //After this, should have downloaded all the actual game files, now its time to move them into one folder


            string exeLocation = Directory.GetCurrentDirectory();
            string depotsFolder = exeLocation + "\\depots";
            string[] folder441 = Directory.GetDirectories(depotsFolder + "\\441");

            DirectoryInfo data441 = new DirectoryInfo(folder441[0]);

            string[] folder440 = Directory.GetDirectories(depotsFolder + "\\440");

            DirectoryInfo data440 = new DirectoryInfo(folder440[0]);

            Console.Out.WriteLine(folder440[0]);

            string[] folder232251 = Directory.GetDirectories(depotsFolder + "\\232251");

            DirectoryInfo data232251 = new DirectoryInfo(folder232251[0]);

            Console.Out.WriteLine("Copying General TF2 files...");
            CopyFilesRecursively(data440, data441);
            Console.Out.WriteLine("Done!");
            /*Console.Out.WriteLine("Cleaning up...");
            Directory.Delete(data441.FullName, true);*/


            Console.Out.WriteLine("Copying Windows-specific Data...");
            CopyFilesRecursively(data232251, data441);
            Console.Out.WriteLine("Done!");
            /*Console.Out.WriteLine("Cleaning up...");
            Directory.Delete(data232251.FullName, true);*/

            //Should have done the core parts of downloading off of Steam servers, now it's time to use shady russian hacks!


            using (var client = new WebClient())
            {
                Console.Out.WriteLine("Downloading revemu...");
                client.DownloadFile("https://www.dropbox.com/s/md2nsxfz9xqw8y7/tf2.zip?dl=1", depotsFolder + "\\tf2.zip");
                Console.Out.WriteLine("Done!");
            }
            
            // if for some reason this doesn't work, comment it out
            using (ZipFile zip1 = ZipFile.Read(depotsFolder + "\\tf2.zip"))
            {
                Console.Out.WriteLine("Extracting revemu...");
                // here, we extract every entry, but we could extract conditionally
                // based on entry name, size, date, checkbox status, etc.  
                foreach (ZipEntry e in zip1)
                {
                    e.Extract(depotsFolder + "\\tf2revemu", ExtractExistingFileAction.OverwriteSilently);
                }
            }
            Console.Out.WriteLine("Done!");

            DirectoryInfo revemu = new DirectoryInfo(depotsFolder + "\\tf2revemu");

            Console.Out.WriteLine("Copying revemu to the tf2 folder...");
            CopyFilesRecursively(revemu, data441);
            Console.Out.WriteLine("Done!");
            /*Console.Out.WriteLine("Cleaning up...");
            Directory.Delete(revemu.FullName, true);
            Console.Out.WriteLine("Done!");*/

            string[] arrLine = File.ReadAllLines(data441.FullName + "\\rev.ini");

            string name;

            Console.Out.Write("\nPlease enter your desired in-game name: ");
            name = Console.In.ReadLine();

            arrLine[120] = "PlayerName=" + name + "\n";
            arrLine[92] = "SteamUser=" + name + "\n";

            File.WriteAllLines(data441.FullName + "\\rev.ini", arrLine);*/
            //comment to here
                
            Console.Out.WriteLine("\nFinished installing and setting everything up - run revLoader.exe not hl2.exe, with admin rights (if it doesn't work). Also I can't stress how important this is:\n TURN OFF STEAM BEFORE RUNNING REVLOADER BECAUSE YOUR ACTUAL PROFILE INFO WILL BE USED AND YOU MIGHT RISK GETTING VAC'd, B&'d, V&'d OR ANY OTHER ARRAY OF THINGS THAT MAY BEFALL YOUR STEAM ACCOUNT, PC, FILES, FAMILY, FRIENDS OR CAT");
            Console.Out.WriteLine("Press any key to continue and exit...");
            Console.In.Read();
        }


        static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (FileInfo file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name), true);
        }

        static string ReadPassword()
        {
            string password = "";
            ConsoleKeyInfo info = Console.ReadKey(true);
            while (info.Key != ConsoleKey.Enter)
            {
                if (info.Key != ConsoleKey.Backspace)
                {
                    Console.Write("*");
                    password += info.KeyChar;
                }
                else if (info.Key == ConsoleKey.Backspace)
                {
                    if (!string.IsNullOrEmpty(password))
                    {
                        // remove one character from the list of password characters
                        password = password.Substring(0, password.Length - 1);
                        // get the location of the cursor
                        int pos = Console.CursorLeft;
                        // move the cursor to the left by one character
                        Console.SetCursorPosition(pos - 1, Console.CursorTop);
                        // replace it with space
                        Console.Write(" ");
                        // move the cursor to the left by one character again
                        Console.SetCursorPosition(pos - 1, Console.CursorTop);
                    }
                }
                info = Console.ReadKey(true);
            }
            // add a new line because user pressed enter at the end of their password
            Console.WriteLine();
            return password;
        }

        static void Download( string[] args )
        {
            if ( args.Length == 0 )
            {
                PrintUsage();
                return;
            }

            DebugLog.Enabled = false;

            ConfigStore.LoadFromFile(Path.Combine(Environment.CurrentDirectory, "DepotDownloader.config"));

            bool bDumpManifest = HasParameter( args, "-manifest-only" );
            uint appId = GetParameter<uint>( args, "-app", ContentDownloader.INVALID_APP_ID );
            uint depotId = GetParameter<uint>( args, "-depot", ContentDownloader.INVALID_DEPOT_ID );
            ContentDownloader.Config.ManifestId = GetParameter<ulong>( args, "-manifest", ContentDownloader.INVALID_MANIFEST_ID );

            if ( appId == ContentDownloader.INVALID_APP_ID )
            {
                Console.WriteLine( "Error: -app not specified!" );
                return;
            }

            if (depotId == ContentDownloader.INVALID_DEPOT_ID && ContentDownloader.Config.ManifestId != ContentDownloader.INVALID_MANIFEST_ID)
            {
                Console.WriteLine("Error: -manifest requires -depot to be specified");
                return;
            }

            ContentDownloader.Config.DownloadManifestOnly = bDumpManifest;

            int cellId = GetParameter<int>(args, "-cellid", -1);
            if (cellId == -1)
            {
                cellId = 0;
            }

            ContentDownloader.Config.CellID = cellId;
            ContentDownloader.Config.BetaPassword = GetParameter<string>(args, "-betapassword");

            string fileList = GetParameter<string>(args, "-filelist");
            string[] files = null;

            if ( fileList != null )
            {
                try
                {
                    string fileListData = File.ReadAllText( fileList );
                    files = fileListData.Split( new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries );

                    ContentDownloader.Config.UsingFileList = true;
                    ContentDownloader.Config.FilesToDownload = new List<string>();
                    ContentDownloader.Config.FilesToDownloadRegex = new List<Regex>();

                    foreach (var fileEntry in files)
                    {
                        try
                        {
                            Regex rgx = new Regex(fileEntry, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                            ContentDownloader.Config.FilesToDownloadRegex.Add(rgx);
                        }
                        catch
                        {
                            ContentDownloader.Config.FilesToDownload.Add(fileEntry);
                            continue;
                        }
                    }

                    Console.WriteLine( "Using filelist: '{0}'.", fileList );
                }
                catch ( Exception ex )
                {
                    Console.WriteLine( "Warning: Unable to load filelist: {0}", ex.ToString() );
                }
            }

            string username = GetParameter<string>(args, "-username") ?? GetParameter<string>(args, "-user");
            string password = GetParameter<string>(args, "-password") ?? GetParameter<string>(args, "-pass");
            ContentDownloader.Config.InstallDirectory = GetParameter<string>(args, "-dir");
            ContentDownloader.Config.DownloadAllPlatforms = HasParameter(args, "-all-platforms");
            ContentDownloader.Config.VerifyAll = HasParameter(args, "-verify-all") || HasParameter(args, "-verify_all") || HasParameter(args, "-validate");
            ContentDownloader.Config.MaxServers = GetParameter<int>(args, "-max-servers", 20);
            ContentDownloader.Config.MaxDownloads = GetParameter<int>(args, "-max-downloads", 4);
            string branch = GetParameter<string>(args, "-branch") ?? GetParameter<string>(args, "-beta") ?? "Public";
            var forceDepot = HasParameter(args, "-force-depot");

            ContentDownloader.Config.MaxServers = Math.Max(ContentDownloader.Config.MaxServers, ContentDownloader.Config.MaxDownloads);

            if (username != null && password == null)
            {
                Console.Write("Enter account password for \"{0}\": ", username);
                password = Util.ReadPassword();
                Console.WriteLine();
            }
            else if (username == null)
            {
                Console.WriteLine("No username given. Using anonymous account with dedicated server subscription.");
            }

            if (ContentDownloader.InitializeSteam3(username, password))
            {
                ContentDownloader.DownloadApp(appId, depotId, branch, forceDepot);
                ContentDownloader.ShutdownSteam3();
            }
        }

        static int IndexOfParam( string[] args, string param )
        {
            for ( int x = 0 ; x < args.Length ; ++x )
            {
                if ( args[ x ].Equals( param, StringComparison.OrdinalIgnoreCase ) )
                    return x;
            }
            return -1;
        }
        static bool HasParameter( string[] args, string param )
        {
            return IndexOfParam( args, param ) > -1;
        }

        static T GetParameter<T>(string[] args, string param, T defaultValue = default(T))
        {
            int index = IndexOfParam(args, param);

            if (index == -1 || index == (args.Length - 1))
                return defaultValue;

            string strParam = args[index + 1];

            var converter = TypeDescriptor.GetConverter(typeof(T));
            if( converter != null )
            {
                return (T)converter.ConvertFromString(strParam);
            }
            
            return default(T);
        }

        static void PrintUsage()
        {
            Console.WriteLine( "\nUsage: depotdownloader <parameters> [optional parameters]\n" );

            Console.WriteLine( "Parameters:" );
            Console.WriteLine("\t-app <#>\t\t\t\t- the AppID to download.");            
            Console.WriteLine();

            Console.WriteLine( "Optional Parameters:" );
            Console.WriteLine( "\t-depot <#>\t\t\t- the DepotID to download." );
            Console.WriteLine( "\t-cellid <#>\t\t\t- the overridden CellID of the content server to download from." );
            Console.WriteLine( "\t-username <user>\t\t\t- the username of the account to login to for restricted content." );
            Console.WriteLine( "\t-password <pass>\t\t\t- the password of the account to login to for restricted content." );
            Console.WriteLine( "\t-dir <installdir>\t\t\t- the directory in which to place downloaded files." );
            Console.WriteLine( "\t-filelist <filename.txt>\t\t- a list of files to download (from the manifest). Can optionally use regex to download only certain files." );
            Console.WriteLine( "\t-all-platforms\t\t\t- downloads all platform-specific depots when -app is used." );
            Console.WriteLine( "\t-manifest-only\t\t\t- downloads a human readable manifest for any depots that would be downloaded." );
            Console.WriteLine( "\t-beta <branchname>\t\t\t\t- download from specified branch if available (default: Public)." );
            Console.WriteLine( "\t-betapassword <pass>\t\t\t- branch password if applicable." );
            Console.WriteLine( "\t-manifest <id>\t\t\t- manifest id of content to download (requires -depot, default: current for branch)." );
            Console.WriteLine( "\t-max-servers <#>\t\t\t- maximum number of content servers to use. (default: 8)." );
            Console.WriteLine( "\t-max-downloads <#>\t\t\t- maximum number of chunks to download concurrently. (default: 4)." );
        }
    }
}
