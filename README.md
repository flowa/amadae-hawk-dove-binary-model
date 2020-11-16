# Amadae Hawk Dove binary model simulation

## Synopsis

The repository contains simulation for Amadae Hawk Dove Binary Model by [S.M. Amadae](https://amadae.com/).

* TODO: Explain the model shortly

The simulation is related to the article(s):

* TODO: Put proper reverence here

The implementation of the simulation is funded by:

* TODO: Add funder(s)

---

## Implementation & running simulation

Simulation is implemented with F# using Fable and React. Bundling is done with Webpack.

At the moment you can run simulation only on your own machine. Download this repo (or use `git clone`) and follow instructions below.

* TODO: Release as github pages.

## 1. Install prerequisites

You need to have dotnet SDK and node.js Installed to your machine.

* [Download and install .NET Core SDK](https://www.microsoft.com/net/download/core) 3.0 or higher
* [Download and install node.js](https://nodejs.org)
  * Needed if you want to run UI Client
* Download and install PowerShell
    * Windows 10+ (you should have it)
    * [For Mac](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell-core-on-macos?view=powershell-7)
    * [For Linux](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell-core-on-linux?view=powershell-7)
    * Needed only if you want to convert result JSON files into one csv

## 2. Building and running the app UI client

In the repository root folder in shell (bash, cmd, etc.):

* Install JS dependencies: `npm install`
* Install F# dependencies: `npm start`
* After the first compilation is finished, in your browser open: http://localhost:8080/

Any modification you do to the F# code will be reflected in the web page after saving.

## 3. Building and running the simulation runner client

### 3.1 You can run simulation with multiple setups:

Run this command `dotnet run <runsCount: int> <agents: int> <stage2Rounds: int> <redAgentSetup: int list> <expectedHawkPercents: int list>`
in ```CliRunner``` folder.

where:

* `runs` (integer) How many time simulation is run. Example 100
* `agentCount` (integer) How many agents is used in each simulation". Example 200
* `stage2Rounds` (integer) How many rounds of simulation is ran. Example 250
* `redAgentSetup` (integer list, delimiter = ';'). Example "10;30;50"
* `expectedHawkPercents` (integer list, delimeter = ';'). Example "10;30;50;70;90". 

Simulation is run once for each redAgentSetup and for each expectedHawkPercents. E.g. if you have 9 values in both lists, simulation is run 81 times. 
Each `expectedHawkPercents` is used to setup distribution of Hawks and Doves in the first round and it is used calculate Payoff.

For example:
```
dotnet run 100 200 250 "10;50;90" "10;30;50;70;90"
```

This command will run simulation 100 times for 200 agents with 250 rounds with 15 different 
configuration (for each 3 red agent for each 5 NMSE setup). 

Each file will be saved to output folder. 

### 3.2. Convert results into single CSV file

In MaxOs and Linux, run (note you need to have PowerShell installed and in path):
```
pwsh -f output-to-csv.ps1
```

In Windows, open PowerShell promp in CliRunner folder and run:
```
. .\output-to-csv.ps1
```

**NOTE: I YOU WANT TO RUN SETUP MULTIPLE TIMES WITH DIFFERENT SETUPS YUO MUST DELETE FOLDER MANUALLY**

## Optional tooling

If you want to edit code good options are:
* Visual Studio
* Visual Studio Code with [Ionide](http://ionide.io/)
* [JetBrains Rider](https://www.jetbrains.com/rider/).


----

## Project structure

### npm

JS dependencies are declared in `package.json`, while `package-lock.json` is a lock file automatically generated.

### Webpack

[Webpack](https://webpack.js.org) is a JS bundler with extensions, like a static dev server that enables hot reloading on code changes. Fable interacts with Webpack through the `fable-loader`. Configuration for Webpack is defined in the `webpack.config.js` file. Note this sample only includes basic Webpack configuration for development mode, if you want to see a more comprehensive configuration check the [Fable webpack-config-template](https://github.com/fable-compiler/webpack-config-template/blob/master/webpack.config.js).

### F#

The sample contains three F# projects:

* UI client is located in `src`-folder
* UI Tests are in `test`-folder
* Commandline runner is in `CliRunner`-folder

### Web assets

The `index.html` file and other assets like an icon can be found in the `public` folder.
