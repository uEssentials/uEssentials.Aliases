/*
 *  This file is part of uEssentials project.
 *      https://uessentials.github.io/
 *
 *  Copyright (C) 2015-2016  Leonardosc
 *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License along
 *  with this program; if not, write to the Free Software Foundation, Inc.,
 *  51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Essentials.Api;
using Essentials.Api.Command;
using Essentials.Api.Command.Source;
using Essentials.Api.Module;
using Essentials.Common;
using Essentials.Common.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Essentials.Modules.Aliases
{
    [ModuleInfo(
        Name = "uEssentials.Aliases",
        Author = "Leonardosc",
        Version = "$ASM_VERSION"
    )]
    public class AliasesModule : EssModule {

        public List<CommandAlias> RegisteredAliases { get; } = new List<CommandAlias>();
        private string _aliasesPath;

        public override void OnLoad() {
            Logger.LogInfo($"Enabling Aliases v{Info.Version}");

            _aliasesPath = Path.Combine(Folder, "aliases.json");

            if (!File.Exists(_aliasesPath)) {
                CommandAlias[] defaultAliases = {
                    new CommandAlias("aliastest", "Just a test", null, new [] {"broadcast hi"}, null),
                    new CommandAlias("aliastest2", "Just a test", null, new [] {"broadcast hi2"}, null),
                };

                JsonUtil.Serialize(_aliasesPath, defaultAliases);
            }

            try {
                LoadAliases(_aliasesPath);
                RegisterAliases();
            } catch (Exception cw) {
                Logger.LogError("An error ocurred while loading aliases.");
                Logger.LogError("Make sure that you aliases.json are in correct format,");
                Logger.LogError("then use '/reloadaliases' to try again.");
                Logger.LogError(cw.ToString());
            }

            UEssentials.CommandManager.Register(ReloadAliasesCommand);
        }

        public override void OnUnload() {
            Logger.LogInfo($"Disabling Aliases v{Info.Version}");
        }

        [CommandInfo(
            Name = "reloadaliases",
            Description = "Reload aliases",
            Permission = "essentials.aliases.reloadaliases"
        )]
        public CommandResult ReloadAliasesCommand(ICommandSource src, ICommandArgs args) {
            src.SendMessage("Reloading aliases...");
            var aliasBak = new List<CommandAlias>(RegisteredAliases);
            RegisteredAliases.Clear();

            try {
                LoadAliases(_aliasesPath);
                aliasBak.ForEach(UEssentials.CommandManager.Unregister);
                RegisterAliases();
                src.SendMessage("Sucessfully reloaded!");
            } catch (Exception cw) {
                RegisteredAliases.AddRange(aliasBak); // Restore aliases
                return CommandResult.Error("Error found: {0}", cw.Message);
            }
            
            return CommandResult.Success();
        }

        private void LoadAliases(string path) {
            var rawAliases = JArray.Parse(File.ReadAllText(path));

            // Load aliases 
            foreach (var jsonAlias in rawAliases) {
                var alias = JsonConvert.DeserializeObject<CommandAlias>(jsonAlias.ToString());
                if (alias.Name == null) {
                    throw new ArgumentException($"Name of alias is null or invalid. Alias: \n{jsonAlias}");
                }
                if (alias.Commands == null || alias.Commands.Length == 0) {
                    throw new ArgumentException($"Commands of alias '{alias.Name}' is null or empty.");
                }
                if (RegisteredAliases.Any(a => a.Name.EqualsIgnoreCase(alias.Name))) {
                    throw new ArgumentException($"An alias with name '{alias.Name}' is already registered.");
                }
                RegisteredAliases.Add(alias);
            }
        }

        private void RegisterAliases() {
            // Register aliases
            var commandManager = UEssentials.CommandManager;

            RegisteredAliases.ForEach(entry => {
                commandManager.Register(entry); 
                #if DEBUG
                    Logger.LogInfo($"Registered alias: {entry.Name}");
                #endif
            });
        }
    }
}
