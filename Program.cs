using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Newtonsoft.Json;

namespace BlockMuteSync
{
    internal class Program
    {
        public static string ini_file = "BlockMuteSync.ini";

        [STAThread]
        private static void Main()
        {
            TwitterApi.Login(new IniSettings(new FileInfo(ini_file)));
            if (TwitterApi.TwitterOAuth.User.Token == null) return;

            HashSet<string> blocklist = new HashSet<string>();
            HashSet<string> mutelist = new HashSet<string>();

            Console.WriteLine("Loading login info...");
            string myId = TwitterApi.getMyId();

            string readLine;
            if (!string.IsNullOrEmpty(myId))
            {
                UserIdsObject result;

                Console.Write("Do you have backup of blocklist? (Y/N)");
                readLine = Console.ReadLine();
                if ((readLine != null) && readLine.ToUpper().Trim().Equals("Y"))
                {
                    Console.Write("Enter path of your blocklist\n: ");
                    string input = Console.ReadLine();
                    if ((input != null) && File.Exists(input.Replace("\"", "")))
                        blocklist.UnionWith(File.ReadAllText(input.Replace("\"", "")).Split(','));
                }
                else
                {
                    Console.WriteLine("Get My Block List... (Max 250000 per 15min)");
                    result = JsonConvert.DeserializeObject<UserIdsObject>(TwitterApi.getMyBlockList("-1"));
                    while (result != null)
                    {
                        blocklist.UnionWith(result.ids);
                        if (result.next_cursor != 0)
                            result = JsonConvert.DeserializeObject<UserIdsObject>(TwitterApi.getMyBlockList(result.next_cursor_str));
                        else
                            break;
                    }
                }

                Console.WriteLine("Get My Mute List...");
                result = JsonConvert.DeserializeObject<UserIdsObject>(TwitterApi.getMyMuteList("-1"));
                while (result != null)
                {
                    mutelist.UnionWith(result.ids);
                    if (result.next_cursor != 0)
                        result = JsonConvert.DeserializeObject<UserIdsObject>(TwitterApi.getMyMuteList(result.next_cursor_str));
                    else
                        break;
                }
            }
            else
            {
                Console.WriteLine("Failed to get your info!");
            }

            Console.WriteLine($"Total {blocklist.Count} block, {mutelist.Count} mute");

            blocklist.ExceptWith(mutelist);

            Console.WriteLine($"Found {blocklist.Count} new blocks");
            Console.Write("Do you want continue sync mute with block? (Y/N) : ");
            readLine = Console.ReadLine();
            if ((readLine != null) && readLine.ToUpper().Trim().Equals("Y"))
                foreach (string s in blocklist)
                {
                    TwitterApi.Mute(s);
                    Thread.Sleep(5000);
                }


            Console.Write("Do you want export your block list? (Y/N) : ");
            readLine = Console.ReadLine();
            if ((readLine != null) && readLine.ToUpper().Trim().Equals("Y"))
                File.WriteAllText("blocklist_" + DateTime.Now.ToString("yyyy-MM-dd_HHmm") + ".csv", string.Join(",", blocklist));
        }
    }
}