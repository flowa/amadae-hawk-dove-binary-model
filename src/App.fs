module App

open Model
open Elmish
open Simulation
// MODEL
let runSimulation (setup)=
    let initialGameState = GameState.FromSetup setup
    let afterSimulatio = initialGameState.SimulateRounds
                            setup.RoundToPlay
                            // GameModes.nashEqlibiumGame
                            GameModes.stage2Game
                            // GameModes.simpleGame
    {
        Setup = setup
        State = afterSimulatio
        ViewState = ShowResults { ShowRound = setup.RoundToPlay }
    }


let init() : State =
    let setup: GameSetup =
        {
            RoundToPlay = 100
            AgentCount = 10
            // In use totaln number
            // ration of Red agentes
            PortionOfRed = 50
            PayoffMatrixType = FromRewardAndCost (10.0, 20.0)
            // StrategySpecs = [Hawk, 3; Dove, 3]
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


            // PayoffMatrixType = Custom [
            //     (Hawk, Hawk), (0.0, 0.0)
            //     (Hawk, Dove), (4.0, 0.0)
            //     (Dove, Hawk), (0.0, 4.0)
            //     (Dove, Dove), (2.0, 2.0)
            // ]
        }
    runSimulation setup

// UPDATE

let update (msg:Msg) (state: State) =
    let setGameSetup (updatedGameSetup: GameSetup) =
        { state with Setup = updatedGameSetup}
    let setField (field: FieldValue) =
        match field with
        | TotalRoundsInGame value ->
            (setGameSetup { state.Setup with RoundToPlay = value })
        | AgentCount value ->
            (setGameSetup { state.Setup with AgentCount = value })
        | PortionOfRed value ->
            (setGameSetup { state.Setup with PortionOfRed = value })
        | BenefitOnVictory value ->
            (setGameSetup {
                    state.Setup with
                        PayoffMatrixType = (state.Setup.PayoffMatrixType.SetV (float value))
                })
        | CostOfLoss value ->
            (setGameSetup {
                    state.Setup with
                        PayoffMatrixType = (state.Setup.PayoffMatrixType.SetC (float value))
                })

        // | f ->
        //     eprintfn "Not implemented %A" f;
        //     state

    match msg with
    | SetValue field -> setField field
    | ShowRound round ->
        { state with
            ViewState = ShowResults { ShowRound = round }}
    | ToInitialization ->
        { state with ViewState = InitGame }
    | RunSimulation ->
        runSimulation state.Setup

        // { state with
        //     ViewState = ShowResults { ShowRound = state.Setup.RoundToPlay }}

open MainView
open Elmish.React

// App
Program.mkSimple init update MainView.view
|> Program.withReactBatched "app"
|> Program.withConsoleTrace
|> Program.run