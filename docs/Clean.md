# Tearing down an NLU service

The NLU.DevOps CLI tool includes a sub-command that allows you to tear down an NLU service. This command is useful in continuous integration scenarios where you may stand up a resource only to validate model performance.

## Getting Started

Run the following command:
```bash
dotnet nlu clean -s luis
```

To make things easier, be sure to use the [`--save-appsettings`](Train.md#-a---save-appsettings) option in the `train` command to ensure an `appsettings.json` file is generated with the NLU service-specific details needed to make this call. Use the [`--delete-appsettings`](#-a---delete-appsettings) to delete the `appsettings.json` file after the resources are cleaned up.

## Detailed Usage

### `-s, --service`
Identifier of the NLU service to run against. Try `luis` for [LUIS](https://www.luis.ai) or `lex` for [Lex](https://aws.amazon.com/lex/).

### `-a, --delete-appsettings`

(Optional) Delete the NLU service-specific configuration overrides that were generated using the `--save-appsettings` option in a `train` command.
