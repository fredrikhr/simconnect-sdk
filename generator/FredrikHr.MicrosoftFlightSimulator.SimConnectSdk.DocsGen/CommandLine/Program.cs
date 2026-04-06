using FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.DocsGen.CommandLine;

var cliCommand = CliCommandDefinition.Instance.RootCommand;
await cliCommand.Parse(args).InvokeAsync()
    .ConfigureAwait(continueOnCapturedContext: false);
