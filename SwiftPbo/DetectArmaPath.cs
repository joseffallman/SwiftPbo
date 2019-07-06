using System;
using System.Collections.Generic;
using Microsoft.Win32;
using System.IO;
using System.Text.RegularExpressions;
using SwiftPbo;

namespace SwiftPbo.ArmaPath
{
    public class DetectArmaPath
    {
        static private Boolean _debug = false;

        /// <summary>
        /// Tries to localize the directory where Arma 3 is installed
        /// </summary>
        /// <returns>ArmaPathInfo object with success and path fields</returns>
        static public ArmaPathInfo SearchDefault()
        {
            ArmaPathInfo retArgs = new ArmaPathInfo();
            List<string> gameDirs = new List<string>();
            string InstallPath = "";

            RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\\Valve\\Steam");
            if (registryKey != null)
            {
                try
                {
                    InstallPath = (string)registryKey.GetValue("InstallPath");
                    Debug("Open registry");
                    Debug(InstallPath);
                }
                catch (Exception)
                {
                    Debug("Value not found");
                }
            }

            // Find ALL steam game directorys
            if (InstallPath.Length > 0)
            {
                string path = System.IO.Path.Combine(InstallPath, "steamapps");
                gameDirs.Add(path);
                string[] libfolders = Directory.GetFiles(path, "libraryfolders.vdf");
                if (libfolders.Length == 1)
                {
                    Debug("Found file: libraryfolders.vdf");
                    string[] lines = File.ReadAllLines(@libfolders[0]);
                    Boolean LibraryFoldersTag = false;
                    Regex LibraryFolderRegex = new Regex("\"\\d\"\\t+\"(.+)\"");
                    foreach (string line in lines)
                    {
                        if(!LibraryFoldersTag && line.Contains("LibraryFolders"))
                        {
                            LibraryFoldersTag = true;
                            continue;
                        }
                        else if(LibraryFoldersTag && line.Contains("}"))
                        {
                            LibraryFoldersTag = false;
                            continue;
                        }

                        if (LibraryFoldersTag)
                        {
                            Match match = LibraryFolderRegex.Match(line);
                            if (match.Success)
                            {
                                string LibraryFolderPath = System.IO.Path.Combine(match.Groups[1].Value, "steamapps");
                                //match.Groups[0].Value;
                                DirectoryInfo dirInfo = new DirectoryInfo(LibraryFolderPath);
                                if (dirInfo.Exists)
                                {
                                    gameDirs.Add(dirInfo.FullName);
                                }
                            }
                        }
                    }
                }

                // In all game direcotrys, parse appmanifest-files and search for Arma 3
                foreach (string dir in gameDirs)
                {
                    foreach (string Appmanifestfile in Directory.GetFiles(dir, "appmanifest*.acf"))
                    {
                        string content = File.ReadAllText(Appmanifestfile);
                        if (content.Contains("\"name\"\t\t\"Arma 3\""))
                        {
                            Debug("Found Arma 3");
                            int i = content.IndexOf("installdir");
                            int PathStart = content.IndexOf("\"", i + 11);
                            int PathEnd = content.IndexOf("\"", PathStart+1);
                            string path1 = content.Substring(PathStart+1, PathEnd - PathStart-1);

                            string ArmaPath = System.IO.Path.Combine(dir, "common", path1);
                            DirectoryInfo ArmaPathInfo = new DirectoryInfo(ArmaPath);
                            if (ArmaPathInfo.Exists)
                            {
                                Debug(ArmaPathInfo.FullName);
                                retArgs.Success = true;
                                retArgs.Path = ArmaPathInfo.FullName;
                                return retArgs;
                            }
                        }
                    }
                }
            }


            return retArgs;
        }

        static public ArmaPathInfo ArmaMpMissionsPath()
        {
            ArmaPathInfo retArgs = new ArmaPathInfo();

            retArgs = SearchDefault();
            if (retArgs.Success)
            {
                retArgs.Path = System.IO.Path.Combine(retArgs.Path, "MPMissions");
            }

            return retArgs;
        }

        static public ArmaPathInfo ArmaMissionsPath()
        {
            ArmaPathInfo retArgs = new ArmaPathInfo();

            retArgs = SearchDefault();
            if (retArgs.Success)
            {
                retArgs.Path = System.IO.Path.Combine(retArgs.Path, "Missions");
            }

            return retArgs;
        }

        static private void Debug(string message)
        {
            if (_debug)
            {
                Console.WriteLine(message);
            }
        }
    }

    public struct ArmaPathInfo
    {
        public Boolean Success;
        public String Path;

        public ArmaPathInfo(Boolean success = false, String path = "")
        {
            Success = success;
            Path = path;
        }
    }
}
