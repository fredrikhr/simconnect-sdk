using FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.DocsGen;

var cliCommand = CliCommandDefinition.Instance.RootCommand;
await cliCommand.Parse(args).InvokeAsync()
    .ConfigureAwait(continueOnCapturedContext: false);
