using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTvActionHub
{
    public static class LuaConfigManager
    {
        private static IEnumerable<string> Libs => [
            "(\"TTvActionHub\", \"TTvActionHub.LuaTools.Audio\").Sounds",
            "(\"TTvActionHub\", \"TTvActionHub.LuaTools.Hardware\").Keyboard",
            "(\"TTvActionHub\", \"TTvActionHub.LuaTools.Hardware\").Mouse",
            "(\"TTvActionHub\", \"TTvActionHub.LuaTools.Stuff\").TwitchChat",
            "(\"TTvActionHub\", \"TTvActionHub.LuaTools.Stuff\").Storage",
            "(\"TTvActionHub\", \"TTvActionHub.LuaTools.Stuff\").Funcs",
            "(\"TTvActionHub\", \"TTvActionHub.LuaTools.Stuff\").Users"
            ];

        private static void GenerateCommandsFile(string conf_path)
        {
            var file = Path.Combine(conf_path, "commands.lua");
            if (File.Exists(file)) return;
            var builder = new StringBuilder();

            foreach (var lib in Libs)
            {
                builder.AppendLine($"local {lib.Split(").").Last()} = import {lib}");
            }
            builder.AppendLine();
            builder.AppendLine("local commands = {}");
            builder.AppendLine();
            builder.AppendLine("commands['test'] = {}");
            builder.AppendLine("commands['test']['action'] =\n" +
                "\tfunction(sender, args)\n\t\tTwitchChat.SendMessageAsync('@'..sender..' -> test')\n\tend");
            builder.AppendLine("commands['test']['timeout'] = 1000 -- 1000 ms");
            builder.AppendLine("commands['test']['perm'] = Users.USERLEVEL.VIEWIER");
            builder.AppendLine();
            builder.AppendLine("return commands");

            File.WriteAllText(file, builder.ToString());
        }

        private static void GenerateRewardsFile(string conf_path)
        {
            var file = Path.Combine(conf_path, "rewards.lua");
            if (File.Exists(file)) return;
            var builder = new StringBuilder();

            foreach (var lib in Libs)
            {
                builder.AppendLine($"local {lib.Split(").").Last()} = import {lib}");
            }
            builder.AppendLine();
            builder.AppendLine("local rewards = {}");
            builder.AppendLine();
            builder.AppendLine("rewards['test'] = {}");
            builder.AppendLine("rewards['test']['action'] =\n" +
                "\tfunction(sender, args)\n\t\tTwitchChat.SendMessageAsync('@'..sender..' -> test')\n\tend");
            builder.AppendLine();
            builder.AppendLine("return rewards");

            File.WriteAllText(file, builder.ToString());
        }

        private static void GenerateTimerActionsFile(string conf_path)
        {
            var file = Path.Combine(conf_path, "timeractions.lua");
            if (File.Exists(file)) return;
            var builder = new StringBuilder();

            foreach (var lib in Libs)
            {
                builder.AppendLine($"local {lib.Split(").").Last()} = import {lib}");
            }
            builder.AppendLine();
            builder.AppendLine("local timeractions = {}");
            builder.AppendLine();
            builder.AppendLine("timeractions['test'] = {}");
            builder.AppendLine("timeractions['test']['action'] =\n" +
                "\tfunction()\n\t\tTwitchChat.SendMessageAsync('Just a test -> test')\n\tend");
            builder.AppendLine("timeractions['test']['timeout'] = 10000 -- 10000 ms");
            builder.AppendLine();
            builder.AppendLine("return timeractions");

            File.WriteAllText(file, builder.ToString());
        }

        private static void GenerateMainFile(string conf_path)
        {
            var file = Path.Combine(conf_path, "config.lua");
            if (File.Exists(file)) return;
            var builder = new StringBuilder();
            builder.AppendLine("local configuration = {}");
            builder.AppendLine();
            builder.AppendLine("configuration['force-relog'] = false");
            builder.AppendLine("configuration['timeout'] = 30000 -- 30000 ms == 30 s");
            builder.AppendLine("configuration['logs'] = false");
            builder.AppendLine("configuration['logs'] = false");
            builder.AppendLine("--configuration['opening-bracket'] = '<'");
            builder.AppendLine("--configuration['closing-bracket'] = '<'");
            builder.AppendLine();
            builder.AppendLine("return configuration");

            File.WriteAllText(file, builder.ToString());
        }

        public static bool CheckConfiguration()
        {
            var conf_path = Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "configs"));
            bool result = true;
            foreach(string file in new string[] { "config.lua", "commands.lua", "rewards.lua", "timeractions.lua" })
            {
                result &= File.Exists(Path.Combine(conf_path.FullName, file));  
            }
            return result;
        }
        public static void GenerateConfigs()
        {
            var conf_path = Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "configs"));
            GenerateCommandsFile(conf_path.FullName);
            GenerateRewardsFile(conf_path.FullName);
            GenerateTimerActionsFile(conf_path.FullName);
            GenerateMainFile(conf_path.FullName);
        }
    }
}
