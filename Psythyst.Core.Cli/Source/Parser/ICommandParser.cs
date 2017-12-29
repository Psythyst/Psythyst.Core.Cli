// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Psythyst.Core.Cli
{
    /// <summary>
    /// ICommandParser Interface.
    /// </summary>
    public interface ICommandParser
    {
        ICommandParser Parent { get; set; }
    
        bool ThrowOnUnexpectedArg { get; set; }

        bool AllowArgumentSeparator { get; set; }

        bool ShowInHelpText { get; set; }

        string Name { get; set; }


        string Description { get; set; }

        CommandOption OptionHelp { get; }

        Action<ICommandParser> Configuration { get; set; }


        CommandOption Option(string Template, string Description, CommandOptionType OptionType);
        CommandOption Option(string Template, string Description, CommandOptionType OptionType, bool Inherited);
        CommandOption Option(string Template, string Description, CommandOptionType OptionType, Action<CommandOption> Configuration);
        CommandOption Option(string Template, string Description, CommandOptionType OptionType, Action<CommandOption> Configuration, bool Inherited);
        IEnumerable<CommandOption> GetOptions();




    
        ICommandParser Command(string Name, Action<ICommandParser> Configuration, bool ThrowOnUnexpectedArg = true);
        IEnumerable<ICommandParser> GetCommands();




        CommandArgument Argument(string Name, string Description, bool MultipleValue = false);
        CommandArgument Argument(string Name, string Description, Action<CommandArgument> Configuration, bool MultipleValue = false);
        IEnumerable<CommandArgument> GetArguments();


        CommandOption HelpOption(string Template);

        void ShowHelp(string CommandName = null);
 
        void ShowHint();




        void OnExecute(Func<int> Invoke);

        int Execute();
    }
}