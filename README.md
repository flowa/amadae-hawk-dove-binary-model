# Amadae Hawk Dove binary model simulation

## Synopsis
The Amadae Hawk-Dove Binary (HDB) model of discrimination uses game theoretic agent-
based modeling to study how localized individual choices result in overarching social patterns.
This model provides a minimalist account of the sufficient conditions to yield a systemic pattern
of discrimination. It uses a Hawk Dove game with the added feature that a binary marker is
applied to otherwise homogenous agents throughout a population to create two groups, with
members of one group with an arbitrary tag (such as Red), and members of the second group
with an arbitrary tag (such as Blue).

[Read full description of the model](https://flowa.github.io/amadae-hawk-dove-binary-model/HDBDocumentation-Feb2022.pdf)

---

This repository contains source code for Amadae Hawk-Dove Binary Model by [S.M. Amadae](https://amadae.com/).

You can try out the simulation: 
* https://flowa.github.io/amadae-hawk-dove-binary-model/

The simulation is related to the article(s):
* [Red Queen and Red King Effects in cultural agent-based modeling: Hawk Dove Binary and Systemic Discrimination](https://www.tandfonline.com/doi/full/10.1080/0022250X.2021.2012668)
* [Binary Labels Reinforce Systemic Discrimination](https://www.noemamag.com/binary-labels-reinforce-systemic-discrimination/)

---

## Implementation & running simulation

You can run simulation either using web client or command line runner:

If you want to run simulation on your own machine: Download this repo (or use `git clone`) and follow instructions below.

Use web client if you want play with the visualized version version of the model. Use commandline runner if you want to get a dataset with multiple runs with certain setups. Commandline runner is also a lot faster than web client.

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

Simulation is implemented with F# using Fable and React. Bundling is done with Webpack.

In the repository root folder in shell (bash, cmd, etc.):

* Install JS dependencies: `npm install`
* Start dev server: `npm start`
* After the first compilation is finished, in your browser open: http://localhost:8080/

Any modification you do to the F# code will be reflected in the web page after saving.

## 3. Building and running the simulation with commandline client

### 3.1 You can run simulation with multiple setups:

Run this command 
`
dotnet run id=<id of the run> runs=<repetitions> agents=<agents count> stage2=<stage1 rounds>-<state 2 mode> red=<red agent persentage> hawk=<hawk persentage> stage1=<stage mode>
`
in `CliRunner` folder.

where:

* `id` (alphanumeric): Identifier for the simulation. Used in file names and included in the simulation result set. E.g. `test`
* `runs` (integer) How many time simulation is run. Example `100`
* `agents` (integer) How many agents is used in each simulation". Example `200`
* `stage2` (specific format: `integer`-`mode name`) How many rounds of simulation is ran with given model. Example `250-stage2_dove` means 250 rounds with stage2_dove mode
* `stage1` (OPTIONAL; specific format: `integer`-`mode name`) How many rounds of simulation is ran with given model. Example `250-stage2_dove` means 250 rounds with stage2_dove mode
* `red` (integer OR integer list, delimiter = ';'). Example `"10;30;50"` or just `10`. Note: Use qoutes (") for the list
* `hawk` (integer list, delimiter = ';'). Example `"10;30;50;70;90"` or `90`. Note: Use qoutes (") for the list

Simulation is run once for each red and for each hawk setups. 

E.g. if you have 9 values in both lists and runs=20, simulation is run for 81 data points 20 times per datapoint each (that's 1620 games in total). Each `hawk` is used to setup distribution of 
Hawks and Doves in the first round and it is used calculate Payoff. Reward is fixed 10, and Cost is 10/<hawk portion>. E.g. if Hawk persentage is 50, when cost is 20 (10/0.5)

#### Example 1:
```
dotnet run id=demo runs=20 agents=200 stage2=250-stage2_dove red=60 hawk=90
```

- Simulation related files will be prefixed demo. 
- Simulation is run 20 times
- There is 200 agent
- Of which 60% is red
- Of which 90% would be HAWKs if all agents used Nash Mixed Strategy Equation (NSME). This means that Reward V=10 and Cost C=11.
- Mode of the games is stage2_dove (see below what this means)
- Stage1 is not run. Note: Stage2 is mandatory and stage1 is optional, this is sligthly unintuitive. It is so for historic reasons. 

#### Example 2:
```
dotnet run id=demo runs=20 agents=200 stage1=100-stage1 stage2=200-stage2 red="10,50,90" hawk="10,50,90"
```

- This command runs simulation 20 times with 9 different setups. Setups are: 
  * 10% red, 10% hawks  
  * 10% red, 50% hawks
  * 10% red, 90% hawks
  * 50% red, 10% hawks
  * 50% red, 50% hawks
  * 50% red, 90% hawks
  * 90% red, 10% hawks
  * 90% red, 50% hawks
  * 90% red, 90% hawks
- For each setup simulation is run 20 times
- Simulation will have two stages: 
  - Stage1 has 100 rounds, and the mode is stage1 (see below)
  - Stage2 has 200 rounds, and the mode is stage2 (see below)
- There will be 200 agents
- The all files generated by the simulation has demo prefix.

The results are written to output folder in JSON-format.

## Results

Results of each simulation is in `CLIRunner/output` folder. 

The folder has one file per simulation setup as JSON file. Use output-to-csv to convert data into csv.

Data for a data point looks like this.
```
{
  "Id": "demo",
  "Runs": 20,
  "AgentCount": 200,
  "RedAgentPercentage": 10,
  "Stage1Rounds": 100,
  "Stage1Mode": "stage1",
  "Stage2Rounds": 200,
  "Stage2Mode": "stage2",
  "HawkPortion": 0.9,
  "PayoffReward": 10.0,
  "PayoffCost": 11.111111111111111111111111111,
  "FirstRoundHawkCountAvg": 179.05,
  "FirstRoundDoveCountAvg": 20.95,
  "FirstSeparationOfColors_Avg": 147.5,
  "FirstSeparationOfColors_Min": 114,
  "FirstSeparationOfColors_Max": 200,
  "FirstSeparationOfColors_Count": 18,
  "FirstSeparationOfColors_P": 0.9,
  "FirstSeparationOfColors_DominatedByRed_Count": 18,
  "FirstSeparationOfColors_DominatedByRed_P": 0.9,
  "FirstSeparationOfColors_DominatedByBlue_Count": 0,
  "FirstSeparationOfColors_DominatedByBlue_P": 0,
  "FirstSeparationOfColors_DominatedByNone_Count": 2,
  "FirstSeparationOfColors_DominatedByNone_P": 0.1,
  "LastRoundSeparationOfColors_DominatedByRed_Count": 2,
  "LastRoundSeparationOfColors_DominatedByRed_P": 0.1,
  "LastRoundSeparationOfColors_DominatedByBlue_Count": 0,
  "LastRoundSeparationOfColors_DominatedByBlue_P": 0,
  "LastRoundSeparationOfColors_DominatedByNone_Count": 18,
  "LastRoundSeparationOfColors_DominatedByNone_P": 0.9
}
```

* The games setups is described in the first attributes (i.e. id, agent count, red portion, etc.)
* First separation of colors refers the first round where every agent of color X played Dove, and every 
agent of color X played Hawk. It is possible that there was no such round. E.g. in the example above 
10% of games had 0 rounds where every agent of color X played Dove, and every agent of color X played 
Hawk.
* Last round separation is the snapshot of the last round. In the example above 90% of the simulation 
runs had no dominating colors in the last round. And in 10% of rounds Red dominated, i.e. Every red agents
played Hawks and every Blue agent played Dove.

There is also a sample of first five simulations are in in `CLIRunner/output` folder in zip file.

Data contains: 

* Round index (zero based) 
* Agent color and id, B = Blue, R=red (E.g. Bb is Blue b (id is hexadecimal number)).
* Other agent color and id (same encoding)
* Chosen strategies D=Dove, H=Hawk. DD means first agents played Dove, the other played Dove. HD means first agent played Hawk, the other played Dove

Currently there is a lot of reporting code commented. This data set was used in the last run. But in earlier simulations a lot mot stats was calculated.

## Game modes

1. `stage1` = NMSE strategy

* Agents don't care about the color. They play as use hawk propability to decide strategy randomly.
    * E.g. if hawk_P = 90%, they play 90% of the time hawk.
    * Note: Even if in this mode agent dont use color to decide how it play it still keep book on the playing strategy per color
* In stage 2 all agents use this in the same colored encounters.

2. `stage2` = Highest expected value (individual history)

*  In this mode, the first time an agent encounters an opposing color, it just flips a coin. Technically observation history is implemented so that, when there is no data, Hawk probability AND Dove probability will be 0.0, and therefore also the expected value both for Hawk and for Dove is 0. When there is data, the agent will count how many times opposing color agent have played Hawk and how many Dove and returns hawk probability and dove probability on the basis of this.
*  Hence, if you use this mode without initialization, this should yield approx. 50% of Hawk and Dove in the first round in opposing color encounters (when no agent has met another agent).
*  If the color distribution is unequal, it's likely that agents just flip a coin in the first couple of rounds.


3. `stage2_dove` = Highest EV - External stats all play Dove

  * In this mode, the first time any agent encounters an opposing color, the agent presumes that hawk_P is 0.0 (external statistics, constant).
  * Hence, all agents play Hawk in the first encounter (not Dove).
  * After the first encounter, agents will play stage 2 game using their own observation history. I.e. if the other played Hawk they play Dove, because 100% of the opposing color players have played Hawk and vice versa.
  * You cannot tell when the agent encounters an opposing color. Actually, there is no guarantee if an agent ever faces an opposing color agent during the simulation. However, when round count is high it's very unlikely that they won't.

4. `stage2_hawk` = Highest EV - External stats all play Hawk

    *  In this mode, the first time any agent encounters an opposing color it presumes that hawk_P is 1.0 (external statistics, constant).
    *  Hence, all agents play Dove in the first encounter (not Dove).
    *  After the first encounter, agents will play stage 2 game using their own observation history just like above.
    *  You cannot tell when the agent encounters an opposing color. 

5. `stage2_half` = Highest EV - External stats 50% play Hawk

    *  In this mode, the first time any agent encounters an opposing color it presumes that hawk_P is 0.5 (external statistics, constant).
    *  Expected distribution of hawks and doves depends on payoff matrix.
    *  After the first encounter, agents will play stage 2 game using their own observation history just like above.
    *  You cannot tell when the agent encounters an opposing color. 

6. `stage2_nsme` = Highest EV - External stats (V/C) play Hawk (50%/50% on tie)

   * In this mode, the first time any agent encounters an opposing color it presumes that hawk_P is NMSE (V / C) (external statistics, computed from payoff matrix).
   * Because of NMSE (V / C) will lead situation where expected value for Hawk and Dove are equal, the all agents will flip a coin on the first round they encountered an opposing color agent
   * After the first encounter, agents will play stage 2 game using their own observation history just like above.
   * You cannot tell when the agent encounters an opposing color.
   
7. `stage2_random` = Highest EV - External stats (V/C) play Hawk (V/C on tie)

   * In this mode, the first time any agent encounters an opposing color it presumes that hawk_P is NMSE (V / C) (external statistics, computed from payoff matrix).
   * Because of NMSE (V / C) will lead situation where expected value for Hawk and Dove are equal, however unlike in the previous case agents plays NSME game. I.e. if C is high, most agents play Dove and vice versa.
   * After the first encounter, agents will play stage 2 game using their own observation history just like above.
      *   Note: after the first encounter, when expected value for Hawk and Dove is equal, agents flip a coin --- they do not play the NMSE game except in the round where they encounter the first time an opposing color agent.
   * You cannot tell when the agent encounters an opposing color

### 3.2. Convert results into single CSV file

In MaxOs and Linux, run (note you need to have PowerShell installed and in path):
```
pwsh -f output-to-csv.ps1
```

In Windows, open PowerShell promp in CliRunner folder and run:
```
. .\output-to-csv.ps1
```

**NOTE:** 
**IT WILL OVERWRITE POSSIBLE EXISTING FILE AND IT WILL CLEAN NOTHING IF NOT NEEDED.**
**USE DIFFERENT ID TO DIFFERENTIATE SETUPS AND RUNS, AND PREPARE TO CLEAN OUTPUT FOLDER MANUALLY**

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