// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Psythyst.Core.Cli
{
    /// <summary>
    /// CommandParserException Class.
    /// </summary>
    public class CommandParserException : Exception
    {
        public CommandParserException(ICommandParser command, string message)
            : base(message)
        {
            Command = command;
        }

        public ICommandParser Command { get; }
    }
}