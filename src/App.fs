module App

open Model
open Elmish
open Simulation
// MODEL
let init() : State =
    let setup: GameSetup =
        {
            RoundToPlay = 100
            // In use totaln number
            // ration of Red agentes
            ColorSpecs = [Red, 10; Blue, 10]
            StrategySpecs = [Hawk, 10; Dove, 10]
            // UseNashPortions
            // CustomPortion [Hawk, 9; Dove, 9]

            // In UI
            // number of rounds of certain type of game
            // - stage 1
            // - stage 2
            // - stage 3
            // PayoffMatric cost
            // Show payoff materic
            // Note:
            //    Technically in game stage thtree there are two
            //    Matrices
            // Add possibility to download simulation data as csv
            // Add summary table
            // - row per stage, avg payoff for per color and per strategy
            // Add cability so see historical round and animation

            PayoffMatrixType = FromRewardAndCost (10.0, 20.0)
            // PayoffMatrixType = Custom [
            //     (Hawk, Hawk), (0.0, 0.0)
            //     (Hawk, Dove), (4.0, 0.0)
            //     (Dove, Hawk), (0.0, 4.0)
            //     (Dove, Dove), (2.0, 2.0)
            // ]
        }
    let initialGameState = GameState.FromSetup setup
    let afterSimulatio = initialGameState.SimulateRounds
                            setup.RoundToPlay
                            // GameModes.nashEqlibiumGame
                            GameModes.stage2Game
                            // GameModes.simpleGame
    {
        Setup = setup
        State = afterSimulatio
    }

// UPDATE

let update (msg:Msg) (state: State) =
    let setGameSetup (updatedGameSetup: GameSetup) =
        { state with Setup = updatedGameSetup}
    let setField (field: FieldValue) =
        match field with
        | MaxRoundsField value -> (setGameSetup { state.Setup with RoundToPlay = value })
        | f ->
            eprintfn "Not implemented %A" f;
            state

    match msg with
    | Set field -> setField field

open MainView
open Elmish.React

// App
Program.mkSimple init update MainView.view
|> Program.withReactBatched "app"
|> Program.withConsoleTrace
|> Program.run