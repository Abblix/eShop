# eShop on Abblix OIDC Server: a worked IdentityServer migration for .NET 10

Microsoft's [eShop](https://github.com/dotnet/eShop) reference application with its OpenID Connect
provider migrated from Duende IdentityServer to
[Abblix OIDC Server](https://github.com/Abblix/Oidc.Server). Everything else is upstream: the same
.NET Aspire orchestration, the same Blazor storefront, the same microservices, the same APIs.

Clone it to see what moving an ASP.NET Core identity provider off IdentityServer actually touches:
client and scope registration, the login and logout screens, claim mapping, signing keys and token
persistence. On the consuming side it touches almost nothing. No resource server was modified and
no authority URL changed; the two client applications gained four statements between them, so that
each names in full the scopes it requests. Every step is explained, with the alternatives that were
rejected and why, in the
[step-by-step migration guide](https://docs.abblix.com/docs/migrate-from-identityserver).

## Why this fork exists

Trying an identity library is normally expensive: you find out what it costs only after you have
wired it into your own system, and by then you have paid for the answer. This fork moves that
expense somewhere else. eShop is public and familiar to most .NET developers, it runs
IdentityServer upstream, and both versions build and run, so the whole comparison is a diff between
two commits of an application you already know rather than a plan you have to imagine.

It is not an argument for leaving Duende IdentityServer. That is a mature product with a decade of
production behind it, and a system running well on it has no reason here. What this fork offers is
the other half of the decision, the one a data sheet cannot give: what the move actually touches,
and where a library that ships fewer batteries hands work back to you.

If you do have your own reason to move, or you are simply looking at what else the ecosystem
offers and would rather try one than read about it, this is the fork to clone and study. It runs
on your machine in a few minutes, and every decision behind it is written up in the guide.

The reasoning behind each step, including the alternatives rejected and why, is in the
[step-by-step migration guide](https://docs.abblix.com/docs/migrate-from-identityserver).

> **Unofficial fork.** Maintained by Abblix LLP. Not affiliated with, endorsed by, or supported by
> Microsoft or the .NET Foundation; their names and logos remain their respective owners'. The
> upstream code stays under its original MIT licence, reproduced unchanged in [LICENSE](LICENSE).
> Abblix OIDC Server is a separate product under its own licence, consumed here as a NuGet package.

## What eShop is

A reference .NET application implementing an e-commerce website using a services-based architecture using [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/).

![eShop Reference Application architecture diagram](img/eshop_architecture.png)

![eShop homepage screenshot](img/eshop_homepage.png)

## Getting Started

This version of eShop is based on .NET 10.

Upstream, including its earlier releases: [dotnet/eShop](https://github.com/dotnet/eShop).

### Prerequisites

- Clone this repository: https://github.com/Abblix/eShop
- [Install & start Docker Desktop](https://docs.docker.com/engine/install/)

#### Windows with Visual Studio
- Install [Visual Studio 2022 version 17.10 or newer](https://visualstudio.microsoft.com/vs/).
  - Select the following workloads:
    - `ASP.NET and web development` workload.
    - `.NET Aspire SDK` component in `Individual components`.
    - Optional: `.NET Multi-platform App UI development` to run client apps

Or

- Run the following commands in a Powershell & Terminal running as `Administrator` to automatically configure your environment with the required tools to build and run this application. (Note: A restart is required and included in the script below.)

```powershell
install-Module -Name Microsoft.WinGet.Configuration -AllowPrerelease -AcceptLicense -Force
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
get-WinGetConfiguration -file .\.configurations\vside.dsc.yaml | Invoke-WinGetConfiguration -AcceptConfigurationAgreements
```

Or

- From Dev Home go to `Machine Configuration -> Clone repositories`. Enter the URL for this repository. In the confirmation screen look for the section `Configuration File Detected` and click `Run File`.

#### Mac, Linux, & Windows without Visual Studio
- Install the latest [.NET 9 SDK](https://dot.net/download?cid=eshop)

Or

- Run the following commands in a Powershell & Terminal running as `Administrator` to automatically configuration your environment with the required tools to build and run this application. (Note: A restart is required after running the script below.)

##### Install Visual Studio Code and related extensions
```powershell
install-Module -Name Microsoft.WinGet.Configuration -AllowPrerelease -AcceptLicense  -Force
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
get-WinGetConfiguration -file .\.configurations\vscode.dsc.yaml | Invoke-WinGetConfiguration -AcceptConfigurationAgreements
```

> Note: These commands may require `sudo`

- Optional: Install [Visual Studio Code with C# Dev Kit](https://code.visualstudio.com/docs/csharp/get-started)
- Optional: Install [.NET MAUI Workload](https://learn.microsoft.com/dotnet/maui/get-started/installation?tabs=visual-studio-code)

> Note: When running on Mac with Apple Silicon (M series processor), Rosetta 2 for grpc-tools. 

### Running the solution

> [!WARNING]
> Remember to ensure that Docker is started

* (Windows only) Run the application from Visual Studio:
 - Open the `eShop.Web.slnf` file in Visual Studio
 - Ensure that `eShop.AppHost.csproj` is your startup project
 - Hit Ctrl-F5 to launch Aspire

* Or run the application from your terminal:
```powershell
dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj
```
then look for lines like this in the console output in order to find the URL to open the Aspire dashboard:
```sh
Login to the dashboard at: http://localhost:19888/login?t=uniquelogincodeforyou
```

> You may need to install ASP.NET Core HTTPS development certificates first, and then close all browser tabs. Learn more at https://aka.ms/aspnet/https-trust-dev-cert

### Azure Open AI

When using Azure OpenAI, inside *eShop.AppHost/appsettings.json*, add the following section:

```json
  "ConnectionStrings": {
    "OpenAi": "Endpoint=xxx;Key=xxx;"
  }
```

Replace the values with your own. Then, in the eShop.AppHost *Program.cs*, set this value to **true**

```csharp
bool useOpenAI = false;
```

Here's additional guidance on the [.NET Aspire OpenAI component](https://learn.microsoft.com/dotnet/aspire/azureai/azureai-openai-component?tabs=dotnet-cli). 

### Use Azure Developer CLI

You can use the [Azure Developer CLI](https://aka.ms/azd) to run this project on Azure with only a few commands. Follow the next instructions:

- Install the latest or update to the latest [Azure Developer CLI (azd)](https://aka.ms/azure-dev/install).
- Log in `azd` (if you haven't done it before) to your Azure account:
```sh
azd auth login
```
- Initialize `azd` from the root of the repo.
```sh
azd init
```
- During init:
  - Select `Use code in the current directory`. Azd will automatically detect the .NET Aspire project.
  - Confirm `.NET (Aspire)` and continue.
  - Select which services to expose to the Internet (exposing `webapp` is enough to test the sample).
  - Finalize the initialization by giving a name to your environment.

- Create Azure resources and deploy the sample by running:
```sh
azd up
```
Notes:
  - The operation takes a few minutes the first time it is ever run for an environment.
  - At the end of the process, `azd` will display the `url` for the webapp. Follow that link to test the sample.
  - You can run `azd up` after saving changes to the sample to re-deploy and update the sample.
  - Report any issues to [azure-dev](https://github.com/Azure/azure-dev/issues) repo.
  - [FAQ and troubleshoot](https://learn.microsoft.com/azure/developer/azure-developer-cli/troubleshoot?tabs=Browser) for azd.

## Contributing

This is a demonstration fork, and the two halves have different homes. Anything about eShop itself
belongs upstream at [dotnet/eShop](https://github.com/dotnet/eShop): changes made here would not
reach the people maintaining it. Anything about the migration or the Abblix provider under it is
welcome as an issue in this repository.

### Sample data

The sample catalog data is defined in [catalog.json](https://github.com/dotnet/eShop/blob/main/src/Catalog.API/Setup/catalog.json). Those product names, descriptions, and brand names are fictional and were generated using [GPT-35-Turbo](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/chatgpt), and the corresponding [product images](https://github.com/dotnet/eShop/tree/main/src/Catalog.API/Pics) were generated using [DALL·E 3](https://openai.com/dall-e-3).

## eShop on Azure

For a version of this app configured for deployment on Azure, please view [the eShop on Azure](https://github.com/Azure-Samples/eShopOnAzure) repo.
