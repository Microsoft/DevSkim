﻿// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.DevSkim.CLI.Commands
{
    public class PackCommand : ICommand
    {
        public PackCommand(string path, string output, bool indent)
        {
            _path = path;
            _outputfile = output;
            _indent = indent;
        }

        public static void Configure(CommandLineApplication command)
        {
            command.Description = "Pack rules into a single file";
            command.HelpOption("-?|-h|--help");

            var locationArgument = command.Argument("[path]",
                                                    "Path to rules");

            var outputArgument = command.Argument("[output]",
                                                    "Output file");

            var indentOption = command.Option("-i|--indent",
                                              "Indent the output json",
                                              CommandOptionType.NoValue);

            command.OnExecute(() =>
            {
                return (new PackCommand(locationArgument.Value,
                                 outputArgument.Value,
                                 indentOption.HasValue())).Run();
            });
        }

        public int Run()
        {
            Verifier verifier = new Verifier(_path);

            if (!verifier.Verify())
                return (int)ExitCode.IssuesExists;

            List<Rule> list = new List<Rule>(verifier.CompiledRuleset.AsEnumerable().Select(x => x.DevSkimRule));

            JsonSerializerSettings settings = new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore };
            settings.Formatting = (_indent) ? Formatting.Indented : Formatting.None;
            settings.Error = delegate (object? sender, Newtonsoft.Json.Serialization.ErrorEventArgs e)
            {
                e.ErrorContext.Handled = true;
            };

            using FileStream fs = File.Open(_outputfile, FileMode.Create, FileAccess.Write);
            using StreamWriter sw = new StreamWriter(fs);
            sw.Write(JsonConvert.SerializeObject(list, settings));
            sw.Close();
            fs.Close();

            return (int)ExitCode.NoIssues;
        }

        private bool _indent;
        private string _outputfile;
        private string _path;
    }
}