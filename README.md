# Chrysalit.Extensions.UserSecrets

This package provide extensions methods for the well-known [Microsoft.Extensions.Configuration.UserSecrets](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.UserSecrets/) package.

Globally the source code is very similar, but the sole purpose is to provide capability to manage `secrets.{env}.json` like the `appsettings.json` and the associated `appsettings.Development.json`.

The later rely on the `IHostEnvironment` configuration.


# Original Problem

It is common that applications are deployed on several environments.
And it is not uncommon willing to debug an application on several environments, whatever during an active development phase, to access production-grade data quality, debug a specific issue ...

While Microsoft.Extensions.Configuration.UserSecrets is adviced for the developper experience, switching secrets from an environment to another is not easy possible.

As developers, we must never commit "secrets" in Git, and in the same time we need to be able to quickly launch an application for several environments, with different connections strings, API passwords and so on.

By default Microsoft.Extensions.Configuration.UserSecrets use only one "secrets.json", even if the SecretId is a folder.


# Solution

The changes provided in this package are so simple that it could be proposed as a PR on the official Dotnet Runtime repository.


# Installation 

## Nuget

The package is available on [Nuget](https://www.nuget.org/)

# Examples

## Using

The examples below uses the following Nuget packages:

- Microsoft.Extensions.Configuration
- Microsoft.Extensions.Configuration.UserSecrets
- Microsoft.Extensions.Hosting
- Chrysalit.Extensions.UserSecrets

All examples below requires the following `using` statements:
```csharp
using Chrysalit.Extensions.UserSecrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
```

## Host.CreateDefaultBuilder()

For this example the `dev` environment is explicitely declared.
Order doesn't matter, `UseEnvironment` may be declared after `ConfigureAppConfiguration`.

```csharp
var host = Host.CreateDefaultBuilder(args)
    .UseEnvironment("dev")
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddUserSecrets<Program>(context);
    })
    .Build();
```

`context` is a `HostBuildContext` object.
By default the `CreateDefaultBuilder` will load the default `secrets.json` file.
But our new `config.AddUserSecrets<Program>(context)` will load the `secrets.dev.json` file and overwrite configuration values.

## AddInMemoryCollection()

Note that `ConfigureHostConfiguration` delegate is called before `ConfigureAppConfiguration` delegate.
Methods may be declared in any order but this one is better for clarity.

```csharp
var host = new HostBuilder()
    .ConfigureHostConfiguration(config =>
    {
        config.AddInMemoryCollection(
        [
            new KeyValuePair<string, string?>(HostDefaults.EnvironmentKey, "dev")
        ]);
    })
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddUserSecrets<Program>(context);
    })
    .Build();
```

# AddEnvironmentVariables()

Configure your application to use the `DOTNET_ENVIRONMENT` environment variable.

```csharp
var host = new HostBuilder()
    .ConfigureHostConfiguration(config =>
    {
        config.AddEnvironmentVariables("DOTNET_");
    })
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddUserSecrets<Program>(context);
    })
    .Build();
```

## Command

Configure your application to use the
`--environment dev`

```csharp
var host = new HostBuilder()
    .ConfigureHostConfiguration(config =>
    {
        config.AddCommandLine(args);
    })
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddUserSecrets<Program>(context);
    })
    .Build();
```

## Example lauchprofile.json

This `launchSettings.json` provides debug configurations for the examples above.

```json
{
  "profiles": {
    "Debug Default": {
      "commandName": "Project"
    },
    "Debug Environment Variable": {
      "commandName": "Project",
      "environmentVariables": {
        "DOTNET_ENVIRONMENT": "dev"
      }
    },
    "Debug CommandLine Args": {
      "commandName": "Project"
      "commandLineArgs": "--environment dev",
    }
  }
}
```

# BuildTime vs RunTime

If you have secrets that you need to embed at buildtime, then you may use Slowcheetah VS extension.

If you have secrets that you need to load only at runtime from your secret storage, use this package instead.

# Limitations

## Library

- Force: Not linked to MSBuild configurations.
- Force: Secrets not leaked on build 
- VS provides the "Manage User Secrets" menu that does automatically loads the "secrets.json" file.


## SlowCheetah

- Force: Provide powerful transformations
- Force: Linked to MSBuild configurations
- Leak the secrets in the build
- Require Administrative privileges to create symbolic links to the physical path of the secrets files.

# Slowcheetah

Slowcheetah is a VS Extension providing transformations for `XML` or `Json` configuration files.

From a configuration file like `appsettings.json`, the extension may create additional `transformations` files depending of the build configuration of your project.

By example, this may be:
- `appsettings.debug.json`
- `appsettings.release.json`

Nevertheless Slowcheetah can only configure files present in your project.

