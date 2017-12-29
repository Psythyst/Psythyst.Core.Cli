// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Psythyst.Core.Cli
{
    /// <summary>
    /// CommandParser Class.
    /// </summary>
    public class CommandParser : ICommandParser
    {
        // Indicates whether the parser should throw an exception when it runs into an unexpected argument.
        // If this field is set to false, the parser will stop parsing when it sees an unexpected argument, and all
        // remaining arguments, including the first unexpected argument, will be stored in RemainingArguments property.

        public CommandParser(bool throwOnUnexpectedArg = true)
        {
            ThrowOnUnexpectedArg = throwOnUnexpectedArg;
            Options = new List<CommandOption>();
            Arguments = new List<CommandArgument>();
            Commands = new List<ICommandParser>();
           // RemainingArguments = new List<string>();
            Invoke = () => 0;
        }


        public ICommandParser Parent { get; set; }
        
        public bool ThrowOnUnexpectedArg { get; set; }

        public CommandOption OptionHelp { get; private set; }

        public CommandOption OptionVersion { get; private set; }

        
        public string Name { get; set; }


        //public string FullName { get; set; }
        
        
        public string Description { get; set; }
        public bool ShowInHelpText { get; set; } = true;

        public bool AllowArgumentSeparator { get; set; }

        public Action<ICommandParser> Configuration { get; set; }

        //public string ExtendedHelpText { get; set; }
        //public string Syntax { get; set; }


        //public CommandOption OptionVersion { get; private set; }
        List<CommandOption> Options;

        List<CommandArgument> Arguments;
        
       // public readonly List<string> RemainingArguments;
        Func<int> Invoke { get; set; }
        Func<string> LongVersionGetter { get; set; }
        Func<string> ShortVersionGetter { get; set; }
        List<ICommandParser> Commands;

        TextWriter Out { get; set; } = Console.Out;
        TextWriter Error { get; set; } = Console.Error;

        public IEnumerable<CommandOption> GetOptions()
        {
            var expr = Options.AsEnumerable();
            ICommandParser rootNode = this;
            while (rootNode.Parent != null)
            {
                rootNode = rootNode.Parent;
                var rootNodeOptions = rootNode.GetOptions().Where(o => o.Inherited);

                expr = expr.Concat(rootNodeOptions);
            }

            return expr;
        }

        public IEnumerable<ICommandParser> GetCommands()
        {
            return Commands;
        }

        public IEnumerable<CommandArgument> GetArguments()
        {
            return Arguments;
        }

        public CommandOption Option(string template, string description, CommandOptionType optionType)
            => Option(template, description, optionType, _ => { }, inherited: false);

        public CommandOption Option(string template, string description, CommandOptionType optionType, bool inherited)
            => Option(template, description, optionType, _ => { }, inherited);

        public CommandOption Option(string template, string description, CommandOptionType optionType, Action<CommandOption> configuration)
            => Option(template, description, optionType, configuration, inherited: false);

        public CommandOption Option(string template, string description, CommandOptionType optionType, Action<CommandOption> configuration, bool inherited)
        {
            var option = new CommandOption(template, optionType)
            {
                Description = description,
                Inherited = inherited
            };
            Options.Add(option);
            configuration(option);
            return option;
        }

        public ICommandParser Command(string name, Action<ICommandParser> configuration,
            bool throwOnUnexpectedArg = true)
        {
            var command = new CommandParser(throwOnUnexpectedArg) { Name = name, Parent = this };
            command.Configuration = configuration;
            Commands.Add(command);
            return command;
        }

        public CommandArgument Argument(string Name, string Description, bool MultipleValue = false)
        {
            return Argument(Name, Description, _ => { }, MultipleValue);
        }

        public CommandArgument Argument(string name, string description, Action<CommandArgument> configuration, bool multipleValues = false)
        {
            var lastArg = Arguments.LastOrDefault();
            if (lastArg != null && lastArg.MultipleValues)
            {
                var message = string.Format("The last argument '{0}' accepts multiple values. No more argument can be added.",
                    lastArg.Name);
                throw new InvalidOperationException(message);
            }

            var argument = new CommandArgument { Name = name, Description = description, MultipleValues = multipleValues };
            Arguments.Add(argument);
            configuration(argument);
            return argument;
        }

        public void OnExecute(Func<int> invoke)
        {
            Invoke = invoke;
        }

        public int Execute()
        {
            return Invoke();
        }

        // Helper method that adds a help option
        public CommandOption HelpOption(string Template)
        {
            // Help option is special because we stop parsing once we see it
            // So we store it separately for further use
            OptionHelp = Option(Template, "Show help information", CommandOptionType.NoValue);

            return OptionHelp;
        }

        // Show short hint that reminds users to use help option
        public void ShowHint()
        {
            if (OptionHelp != null)
            {
                Out.WriteLine(string.Format("Specify --{0} for a list of available options and commands.", OptionHelp.LongName));
            }
        }

        // Show full help
        public void ShowHelp(string commandName = null)
        {
            Out.WriteLine(GetHelpText(commandName));
        }

        public int Execute(params string[] args)
        {
            ICommandParser command = this;
            var commands = new List<ICommandParser>();
            CommandOption option = null;
            IEnumerator<CommandArgument> arguments = null;

            for (var index = 0; index < args.Length; index++)
            {
                var arg = args[index];
                var processed = false;
                if (!processed && option == null)
                {
                    string[] longOption = null;
                    string[] shortOption = null;

                    if (arg.StartsWith("--"))
                    {
                        longOption = arg.Substring(2).Split(new[] { ':', '=' }, 2);
                    }
                    else if (arg.StartsWith("-"))
                    {
                        shortOption = arg.Substring(1).Split(new[] { ':', '=' }, 2);
                    }
                    if (longOption != null)
                    {
                        processed = true;
                        var longOptionName = longOption[0];
                        option = command.GetOptions().SingleOrDefault(opt => string.Equals(opt.LongName, longOptionName, StringComparison.Ordinal));

                        if (option == null)
                        {
                            if (string.IsNullOrEmpty(longOptionName) && !command.ThrowOnUnexpectedArg  && AllowArgumentSeparator)
                            {
                                // skip over the '--' argument separator
                                index++;
                            }

                            HandleUnexpectedArg(command, args, index, argTypeName: "option");
                            break;
                        }

                        // If we find a help/version option, show information and stop parsing
                        if (command.OptionHelp == option)
                        {
                            command.ShowHelp(null);
                            return 0;
                        }
                        //else if (command.OptionVersion == option)
                        //{
                        //    command.ShowVersion();
                        //    return 0;
                        //}

                        if (longOption.Length == 2)
                        {
                            if (!option.TryParse(longOption[1]))
                            {
                                command.ShowHint();
                                throw new CommandParserException(command, $"Unexpected value '{longOption[1]}' for option '{option.LongName}'");
                            }
                            option = null;
                        }
                        else if (option.OptionType == CommandOptionType.NoValue)
                        {
                            // No value is needed for this option
                            option.TryParse(null);
                            option = null;
                        }
                    }
                    if (shortOption != null)
                    {
                        processed = true;
                        option = command.GetOptions().SingleOrDefault(opt => string.Equals(opt.ShortName, shortOption[0], StringComparison.Ordinal));

                        // If not a short option, try symbol option
                        if (option == null)
                        {
                            option = command.GetOptions().SingleOrDefault(opt => string.Equals(opt.SymbolName, shortOption[0], StringComparison.Ordinal));
                        }

                        if (option == null)
                        {
                            HandleUnexpectedArg(command, args, index, argTypeName: "option");
                            break;
                        }

                        // If we find a help/version option, show information and stop parsing
                        if (command.OptionHelp == option)
                        {
                            command.ShowHelp();
                            return 0;
                        }
                        //else if (command.OptionVersion == option)
                        //{
                        //    command.ShowVersion();
                        //    return 0;
                        //}

                        if (shortOption.Length == 2)
                        {
                            if (!option.TryParse(shortOption[1]))
                            {
                                command.ShowHint();
                                throw new CommandParserException(command, $"Unexpected value '{shortOption[1]}' for option '{option.LongName}'");
                            }
                            option = null;
                        }
                        else if (option.OptionType == CommandOptionType.NoValue)
                        {
                            // No value is needed for this option
                            option.TryParse(null);
                            option = null;
                        }
                    }
                }

                if (!processed && option != null)
                {
                    processed = true;
                    if (!option.TryParse(arg))
                    {
                        command.ShowHint();
                        throw new CommandParserException(command, $"Unexpected value '{arg}' for option '{option.LongName}'");
                    }
                    option = null;
                }

                if (!processed && arguments == null)
                {
                    var currentCommand = command;
                    foreach (var subcommand in command.GetCommands())
                    {
                        if (string.Equals(subcommand.Name, arg, StringComparison.OrdinalIgnoreCase))
                        {
                            processed = true;
                            commands.Add(command);
                            command.Execute();
                            command = subcommand;
                            command.Configuration.Invoke(command);
                            break;
                        }
                    }

                    // If we detect a subcommand
                    if (command != currentCommand)
                    {
                        processed = true;
                    }
                }
                if (!processed)
                {
                    if (arguments == null)
                    {
                        arguments = new CommandArgumentEnumerator(command.GetArguments().GetEnumerator());
                    }
                    if (arguments.MoveNext())
                    {
                        processed = true;
                        arguments.Current.Values.Add(arg);
                    }
                }
                if (!processed)
                {
                    HandleUnexpectedArg(command, args, index, argTypeName: "command or argument");
                    break;
                }
            }

            if (option != null)
            {
                command.ShowHint();
                throw new CommandParserException(command, $"Missing value for option '{option.LongName}'");
            }

            foreach(var c in commands){
               // c.Execute();
            }

            return command.Execute();
        }


        public virtual string GetHelpText(string commandName = null)
        {
            var headerBuilder = new StringBuilder("Usage:");
            for (ICommandParser cmd = this; cmd != null; cmd = cmd.Parent)
            {
                headerBuilder.Insert(6, string.Format(" {0}", cmd.Name));
            }

            ICommandParser target;

            if (commandName == null || string.Equals(Name, commandName, StringComparison.OrdinalIgnoreCase))
            {
                target = this;
            }
            else
            {
                target = Commands.SingleOrDefault(cmd => string.Equals(cmd.Name, commandName, StringComparison.OrdinalIgnoreCase));

                if (target != null)
                {
                    headerBuilder.AppendFormat(" {0}", commandName);
                }
                else
                {
                    // The command name is invalid so don't try to show help for something that doesn't exist
                    target = this;
                }

            }

            var optionsBuilder = new StringBuilder();
            var commandsBuilder = new StringBuilder();
            var argumentsBuilder = new StringBuilder();

            var arguments = target.GetArguments().Where(a => a.ShowInHelpText).ToList();
            if (arguments.Any())
            {
                headerBuilder.Append(" [arguments]");

                argumentsBuilder.AppendLine();
                argumentsBuilder.AppendLine("Arguments:");
                var maxArgLen = arguments.Max(a => a.Name.Length);
                var outputFormat = string.Format("  {{0, -{0}}}{{1}}", maxArgLen + 2);
                foreach (var arg in arguments)
                {
                    argumentsBuilder.AppendFormat(outputFormat, arg.Name, arg.Description);
                    argumentsBuilder.AppendLine();
                }
            }

            var options = target.GetOptions().Where(o => o.ShowInHelpText).ToList();
            if (options.Any())
            {
                headerBuilder.Append(" [options]");

                optionsBuilder.AppendLine();
                optionsBuilder.AppendLine("Options:");
                var maxOptLen = options.Max(o => o.Template.Length);
                var outputFormat = string.Format("  {{0, -{0}}}{{1}}", maxOptLen + 2);
                foreach (var opt in options)
                {
                    optionsBuilder.AppendFormat(outputFormat, opt.Template, opt.Description);
                    optionsBuilder.AppendLine();
                }
            }

            var commands = target.GetCommands().Where(c => c.ShowInHelpText).ToList();
            if (commands.Any())
            {
                headerBuilder.Append(" [command]");

                commandsBuilder.AppendLine();
                commandsBuilder.AppendLine("Commands:");
                var maxCmdLen = commands.Max(c => c.Name.Length);
                var outputFormat = string.Format("  {{0, -{0}}}{{1}}", maxCmdLen + 2);
                foreach (var cmd in commands.OrderBy(c => c.Name))
                {
                    commandsBuilder.AppendFormat(outputFormat, cmd.Name, cmd.Description);
                    commandsBuilder.AppendLine();
                }

                if (OptionHelp != null)
                {
                    commandsBuilder.AppendLine();
                    commandsBuilder.AppendFormat($"Use \"{target.Name} [command] --{OptionHelp.LongName}\" for more information about a command.");
                    commandsBuilder.AppendLine();
                }
            }

            if (target.AllowArgumentSeparator)
            {
                headerBuilder.Append(" [[--] <arg>...]");
            }

            headerBuilder.AppendLine();

            var nameAndVersion = new StringBuilder();
            //nameAndVersion.AppendLine(GetFullNameAndVersion());
            nameAndVersion.AppendLine();

            return nameAndVersion.ToString()
                + headerBuilder.ToString()
                + argumentsBuilder.ToString()
                + optionsBuilder.ToString()
                + commandsBuilder.ToString();
                //+ target.ExtendedHelpText;
        }

        private void HandleUnexpectedArg(ICommandParser command, string[] args, int index, string argTypeName)
        {
            if (command.ThrowOnUnexpectedArg)
            {
                command.ShowHint();
                throw new CommandParserException(command, $"Unrecognized {argTypeName} '{args[index]}'");
            }
            else
            {
                // All remaining arguments are stored for further use
                //command.RemainingArguments.AddRange(new ArraySegment<string>(args, index, args.Length - index));
            }
        }

             /* 
        public void ShowVersion()
        {
            Out.WriteLine(FullName);
            Out.WriteLine(LongVersionGetter());
        }*/

        /* 
        public string GetFullNameAndVersion()
        {
            return ShortVersionGetter == null ? FullName : string.Format("{0} {1}", FullName, ShortVersionGetter());
        }*/

        /* 
        public void ShowRootCommandFullNameAndVersion()
        {
            ICommandParser rootCmd = this;
            while (rootCmd.Parent != null)
            {
                rootCmd = rootCmd.Parent;
            }

            //Out.WriteLine(rootCmd.GetFullNameAndVersion());
            Out.WriteLine();
        }*/

                /* 
        public CommandOption VersionOption(string template,
            string shortFormVersion,
            string longFormVersion = null)
        {
            if (longFormVersion == null)
            {
                return VersionOption(template, () => shortFormVersion);
            }
            else
            {
                return VersionOption(template, () => shortFormVersion, () => longFormVersion);
            }
        }*/

        /* 
        public CommandOption VersionOption(string template,
            Func<string> shortFormVersionGetter,
            Func<string> longFormVersionGetter = null)
        {
            // Version option is special because we stop parsing once we see it
            // So we store it separately for further use
            OptionVersion = Option(template, "Show version information", CommandOptionType.NoValue);
            ShortVersionGetter = shortFormVersionGetter;
            LongVersionGetter = longFormVersionGetter ?? shortFormVersionGetter;

            return OptionVersion;
        }*/
    }
}