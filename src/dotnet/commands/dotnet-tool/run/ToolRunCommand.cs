// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.CommandFactory;

namespace Microsoft.DotNet.Tools.Tool.Run
{
    internal class ToolRunCommand : CommandBase
    {
        public const string CommandDelimiter = ", ";
        private readonly string _toolCommandName;
        private readonly LocalToolsCommandResolver _localToolsCommandResolver;
        private readonly IReadOnlyCollection<string> _forwardArgument;

        public ToolRunCommand(
            AppliedOption options,
            ParseResult result,
            LocalToolsCommandResolver localToolsCommandResolver = null)
            : base(result)
        {
            _toolCommandName = options.Arguments.Single();
            _localToolsCommandResolver = localToolsCommandResolver ?? new LocalToolsCommandResolver();
            _forwardArgument = result.UnparsedTokens;
        }

        public override int Execute()
        {
            CommandSpec commandspec = _localToolsCommandResolver.Resolve(new CommandResolverArguments()
            {
                // since LocalToolsCommandResolver is a resolver, and all resolver input have dotnet-
                CommandName = $"dotnet-{_toolCommandName}",
                CommandArguments = _forwardArgument
            });

            if (commandspec == null)
            {
                throw new GracefulException(string.Format(LocalizableStrings.CannotFindCommandName, _toolCommandName));
            }

            var result = CommandFactoryUsingResolver.Create(commandspec).Execute();
            return result.ExitCode;
        }
    }
}
