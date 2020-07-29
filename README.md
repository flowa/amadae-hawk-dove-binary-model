# Amadae Hawk Dove binary model simulation

## TODO

* Round average payoff
* Performance optimization?
* Stats from multiple simulations
    Different color stats (whos submissive), when did it separate, avg payoff


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

## 2. Building and running the app

In the repository root folder in shell (bash, cmd, etc.):

* Install JS dependencies: `npm install`
* Install F# dependencies: `npm start`
* After the first compilation is finished, in your browser open: http://localhost:8080/

Any modification you do to the F# code will be reflected in the web page after saving.

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

The sample only contains two F# files: the project (.fsproj) and a source file (.fs) in the `src` folder.

The tests are located in `test` folder

### Web assets

The `index.html` file and other assets like an icon can be found in the `public` folder.
