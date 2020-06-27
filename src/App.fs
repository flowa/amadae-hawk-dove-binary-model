module App
open Model
open Elmish
open Simulation
// MODEL
let init() : State =
    let setup: GameSetup =
        {
            RoundToPlay = 10
            ColorSpecs = [Red, 2; Blue, 4]
            StrategySpecs = [Hawk, 2; Dowe, 4]
            PayOffTable = Map.ofList [
                (Hawk, Hawk), (0, 0)
                (Hawk, Dowe), (4, 0)
                (Dowe, Hawk), (0, 4)
                (Dowe, Dowe), (2, 2)
            ]
        }
    let initialGameState = GameState.FromSetup setup
    let afterSimulatio = initialGameState.SimulateRounds
                            setup.RoundToPlay
                            GameModes.simpleGame
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