﻿using Microsoft.DotNet.CommandInfrastructure;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Tools.Run.LaunchSettings
{
    public interface ILaunchSettingsProvider
    {
        string CommandName { get; }

        LaunchSettingsApplyResult TryApplySettings(JObject document, JObject model, ref ICommand command);
    }

}
