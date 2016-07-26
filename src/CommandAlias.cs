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

using System.Linq;
using System.Text;
using Essentials.Api;
using Essentials.Api.Command;
using Essentials.Api.Command.Source;
using Essentials.Common;
using Newtonsoft.Json;

namespace Essentials.Modules.Aliases {

    public class CommandAlias : ICommand {

        public string Name { get; }
        public string Usage { get; set; }
        public string Description { get; set; }
        public string[] Commands { get; set; }
        public string[][] ValidInputs { get; set; }

        [JsonIgnore]
        public string Permission { get; set; }

        [JsonIgnore]
        public string[] Aliases { get; set; }

        [JsonIgnore]
        public AllowedSource AllowedSource { get; set; }

        public CommandAlias(string name, string description, string usage, string[] commands, 
                            string[][] validInputs) {
            Name = name;
            Description = description;
            Usage = usage;
            Commands = commands;
            ValidInputs = validInputs;
            AllowedSource = AllowedSource.BOTH;
            Aliases = new string[0];

            Permission = $"essentials.alias.{name}";
        }

        public CommandResult OnExecute(ICommandSource src, ICommandArgs args) {
            // Validate arguments
            if (ValidInputs != null && ValidInputs.Where((t, i) => !t.Any(s => s.Equals(args[i].RawValue))).Any()) {
                return CommandResult.ShowUsage();
            }

            // Execute commands
            Commands.Select(c => new StringBuilder(c)).ForEach(command => {
                command.Replace("$sender", src.DisplayName);
                StringBuilder allArgBuilder = null;

                if (command.ToString().Contains("$arg*")) {
                    allArgBuilder = new StringBuilder();    
                }

                for (var i = 0; i < args.Length; i++) {
                    var current = args[i];
                    command.Replace($"$arg{i}", current.RawValue);

                    if (allArgBuilder == null) continue;
                    if (current.IsString) {
                        allArgBuilder.Append("\"");
                        allArgBuilder.Append(current.RawValue);
                        allArgBuilder.Append("\"");
                    } else {
                        allArgBuilder.Append(current.RawValue);
                    }
                    if (i != args.Length - 1) {
                        allArgBuilder.Append(" ");
                    }
                }

                if (allArgBuilder != null) {
                    command.Replace("$arg*", allArgBuilder.ToString());
                }

                var strCommand = command.ToString();

                if (strCommand.StartsWith("console:")) {
                    strCommand = strCommand.Remove(0, "console:".Length); // remove 'console:' prefix
                    UEssentials.ConsoleSource.DispatchCommand(strCommand);
                } else {
                    src.DispatchCommand(command.ToString());
                }
            });
            return CommandResult.Success();
        }

        public override string ToString() {
            return $"Name: {Name}, Description: {Description}, ValidInputs: {ValidInputs}, Usage: {Usage}, Commands: {Commands}";
        }
    }

}