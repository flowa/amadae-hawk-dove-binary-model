module App

open System
open Model
open Elmish
open Browser
open Simulation
open ModelExtensions
// module Model =


// MODEL
let runSimulationAsync (setup: GameSetup) =
    promise {
        let initialGameState = setup.ToInitialGameState()
        let initialAgents = setup.GenerateAgents()
        let! afterSimulation = initialGameState.SimulateRoundsPromise initialAgents
        return afterSimulation
    }


let init () =
    let setup =
        {
            GameParameters = {
                AgentCount = 100
                PortionOfRed = 50
                PayoffMatrix = FromRewardAndCost (10.0m, 20.0m)
            }
            SimulationFrames = [
                {
                    SimulationFrame.RoundCount = 0
                    SetPayoffForStage = id
                    StageName = "Stage 1"
                    ModeLabel = "Mode: Color-blind play"
                    StrategyInitFnName = SimulationStageNames.ProbabilisticNSME
                    MayUseColor = false
                }
                {
                     SimulationFrame.RoundCount = 100
                     SetPayoffForStage = id
                     StageName = "Stage 2"
                     ModeLabel = "Mode: Play cued by interactant’s color"
                     StrategyInitFnName = SimulationStageNames.HighestExpectedValueOnBasedOfHistory_NsmeHawk_UseNashOnExpectedValueTieWhenNoHistory
                     MayUseColor = true
                }
            ]
        }
    let initialGameState = setup.ToInitialGameState()
    {
        Setup = setup
        GameState = initialGameState
        ViewState = InitGame
        PlayAnimation = false
    }, Cmd.none

// UPDATE

let update (msg:Msg) (state: State) =
    let setGameSimulationFrames (simulationFrames: SimulationFrame list ) =
        let updatedSetup = { state.Setup with SimulationFrames  = simulationFrames} 
        { state with Setup = updatedSetup }

    let setGameParams (updatedGameParams: GameParameters) =
        let updatedSetup = { state.Setup with GameParameters = updatedGameParams } 
        { state with Setup = updatedSetup }
    let setField (field: FieldValue) =
        match field with
        | RoundCountOfStage (stage, value) ->
            (setGameSimulationFrames
                (state.Setup.SimulationFrames
                |> List.map
                    (fun frame ->
                        if frame.StageName = stage then
                            { frame with RoundCount = value }
                        else frame)))
        | ModeOfStage (stage, value) ->
            (setGameSimulationFrames
                (state.Setup.SimulationFrames
                |> List.map
                    (fun frame ->
                        if frame.StageName = stage then
                            { frame with StrategyInitFnName = value }
                        else frame)))
        | AgentCount value ->
            (setGameParams { state.Setup.GameParameters with AgentCount = value })
        | PortionOfRed value ->
            (setGameParams { state.Setup.GameParameters with PortionOfRed = value })
        | BenefitOnVictory value ->
            (setGameParams {
                    state.Setup.GameParameters with
                        PayoffMatrix = (state.Setup.PayoffMatrix.SetV (decimal value))
                })
        | CostOfLoss value ->
            (setGameParams {
                    state.Setup.GameParameters with
                        PayoffMatrix = (state.Setup.PayoffMatrix.SetC (decimal value))
                })

    match msg with
    | SetValue field -> (setField field), Cmd.none
    | ShowRound round ->
        { state with ViewState = ShowResults round }, Cmd.none
    | ToInitialization ->
        { state with
            ViewState = InitGame
            PlayAnimation = false }, Cmd.none
    | RunSimulation ->
        let runSimulation () =
                promise {
                    printfn "starting"
                    do! Promise.sleep 200
                    let! results = runSimulationAsync state.Setup
                    printfn "beforeDispatch"
                    return (OnSimulationCompleted results)
                }
        {state with ViewState = Loading},
        Cmd.OfPromise.perform runSimulation () id

    | OnSimulationCompleted gameState ->
        { state with
            ViewState = ShowResults state.Setup.RoundsToPlay
            GameState = gameState}, Cmd.none
    | Tick _ ->
        let maxRound = state.Setup.RoundsToPlay
        let currentRound = state.CurrentRound
        let updatedState =
            match (state.PlayAnimation, state.ViewState) with
            | true, ShowResults round when maxRound > currentRound ->
                { state with ViewState = ShowResults (round + 1)}
            | true, ShowResults _ when maxRound = currentRound ->
                { state with PlayAnimation = false }
            | _ -> state
        updatedState, Cmd.none
    | PlayAnimation ->
        {
            state with
                PlayAnimation = true
                ViewState =
                    if state.CurrentRound = state.Setup.RoundsToPlay then
                        ShowResults 1
                    else
                        state.ViewState
        }, Cmd.none

    | StopAnimation ->
        { state with PlayAnimation = false }, Cmd.none

open MainView
open Elmish.React

let timer initial =
    let sub dispatch =
        window.setInterval
            (fun _ -> dispatch (Tick System.DateTime.Now)
            , 500) |> ignore
    Cmd.ofSub sub

Program.mkProgram init update MainView.view
|> Program.withReactBatched "app"
|> Program.withSubscription timer
|> Program.run