module MainView

open Model
open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props
open Fulma

// VIEW (rendered with React)
module RoundSlider =
  open Fulma.Extensions.Wikiki
  let slider (model: State) (dispatch) =
    let max = float (model.Setup.RoundToPlay)
    let onChange (e: Browser.Types.Event) =
        dispatch (ShowRound (int e.Value))
    div [ ClassName "block"]
        [ Slider.slider
            [
              Slider.IsFullWidth
              Slider.Max max
              Slider.Min 1.0
              Slider.Value (float model.CurrentRound)
              Slider.OnChange onChange
            ]
        ]
module SettingsForm =
    type NumberFieldProps =
        {
            Label: string
            Value: int
            OnChange: (int -> FieldValue)
        }
    let view (model: State) (dispatch: Msg -> unit) =
        let strToInt s =
            match s with
            | "" -> 0
            | s -> int s
        let numberField
            (props: NumberFieldProps) =
            Field.div
                [Field.IsHorizontal]
                [
                    div [ClassName "field-label is-small"]
                        [
                            Label.label [Label.CustomClass "is-expanded"] [ str props.Label ]
                        ]
                    div [ClassName "field-body"]
                        [
                            Field.div [] [
                                Control.div
                                    []
                                    [ Input.number [
                                        Input.Props [ClassName "is-small"]
                                        Input.OnChange
                                            (fun e -> dispatch (SetValue (props.OnChange (strToInt e.Value))))
                                        Input.Value (props.Value.ToString())]
                                    ]
                            ]
                        ]
                ]
        let renderPayoffMatrics (payoff: PayoffMatrix)  =
            let format (player1, player2) =
                sprintf "%.0f / %.0f" player1 player2
            table [ClassName "payoff-summary"]
                [
                    tr  []
                        [
                            th [] []
                            th [ClassName "Hawk"] [str "Hawk"]
                            th [ClassName "Dove"] [str "Dove"]
                        ]
                    tr  []
                        [
                            th [ClassName "Hawk"] [str "Hawk"]
                            td [] [payoff.[Hawk, Hawk] |> format |> str]
                            td [] [payoff.[Hawk, Dove] |> format |> str]
                        ]
                    tr  []
                        [
                            th [ClassName "Dove"] [str "Dove"]
                            td [] [payoff.[Dove, Hawk] |> format |> str]
                            td [] [payoff.[Dove, Dove] |> format |> str]
                        ]
                ]

        let renderColotStats (setting: GameSetup)  =
            let format (player1, player2) =
                sprintf "%.0f / %.0f" player1 player2
            table [ClassName "agent-summary"]
                [
                    tr  []
                        [
                            th [] []
                            th [ClassName "Red"] [str "Red"]
                            th [ClassName "Blue"] [str "Blue"]
                        ]
                    tr  []
                        [
                            th [] [str "Count"]
                            td [] [setting.CountOfRed |> ofInt]
                            td [] [setting.CountOfBlue |> ofInt]
                        ]
                ]

        match model.ViewState with
        | InitGame ->
            div [
                    ClassName "column is-one-quarter"
                    Id "settings"
                ]
                [
                    h1 [] [
                        str "Setting"
                    ]

                    form []
                        [
                            fieldset []
                                [
                                    legend [] [str "Duration"]
                                    numberField
                                        {
                                            Label =    "Rounds"
                                            Value =    model.Setup.RoundToPlay
                                            OnChange = (fun value -> (TotalRoundsInGame value))
                                        }
                                ]
                            fieldset []
                                [
                                    legend [] [str "Agents"]
                                    numberField
                                        {
                                            Label =    "Agent count"
                                            Value =    model.Setup.AgentCount
                                            OnChange = (fun value -> (AgentCount value))
                                        }

                                    numberField
                                        {
                                            Label =    "Reds agents (%)"
                                            Value =    model.Setup.PortionOfRed
                                            OnChange = (fun value -> (PortionOfRed value))
                                        }
                                    renderColotStats model.Setup
                                ]

                            fieldset []
                                [
                                    legend [] [str "Payoff"]
                                    numberField
                                        {
                                            Label =    "Reward (V)"
                                            Value =    (int model.Setup.PayoffMatrixType.VictoryBenefit)
                                            OnChange = (fun value -> (BenefitOnVictory value))
                                        }

                                    numberField
                                        {
                                            Label =    "Cost (C)"
                                            Value =    (int model.Setup.PayoffMatrixType.Cost)
                                            OnChange = (fun value -> (CostOfLoss value))
                                        }
                                    renderPayoffMatrics (model.Setup.PayoffMatrixType.ToMatrix())
                                ]
                        ]
            ]
        | ShowResults viewState ->
            div [ ClassName "column is-one-quarter"; Id "settings" ] [
                h1  []
                    [
                        str "Setting"
                    ]
                Button.button
                    [
                        Button.IsFullWidth
                        Button.OnClick (fun _ -> (dispatch ToInitialization))
                    ]
                    [str "Initialize new game"]

                pre [] [
                    str (sprintf "%A" model.Setup)
                ]
            ]



module ResultTable =
  let agentBox (rounds: int) (model: Agent) =
    let avg =
      match rounds with
      | 0 -> 0.0
      | r -> model.Payoff / (float r)
    div [ ClassName (sprintf "agent-box fade-in %A %A" model.Color model.Strategy)] [
      div [ ClassName "strategy" ] [ str (sprintf "%A" model.Strategy) ]
      div [ ClassName "payoff"]
          [
            div [] [
                span [] [str "Total: "]
                ofFloat model.Payoff
              ]
            div [] [
                span [] [str "Avg: "]
                ofString (sprintf "%.3f" avg)
              ]
          ]
      div [ ClassName "agent-id"] [ str "#"; ofInt model.Id ]
    ]

  let view (model: State) dispatch =
    let totalRounds = model.State.ResolvedRounds.TotalRounds
    let currentRound = model.CurrentRound
    let currentRoundAgents = model.CurrentRoundAgents()

    let agents =
        currentRoundAgents
        |> List.map (agentBox currentRound)
    div [ ClassName "agent-listing" ] agents

// Main view
let view (model: State) dispatch =
  let currentRound = model.CurrentRound
  let currentRound = model.CurrentRound
  let roundData = model
  let resultPanel =
        match model.ViewState with
        | InitGame ->
            Button.button
                [
                    Button.Size IsLarge
                    Button.OnClick (fun _ -> dispatch RunSimulation)
                ]
                [ str "Run simulation" ]
        | ShowResults { ShowRound = round } ->
            ofList [
              h1 [] [ str (sprintf "Results (Round %i/%i)" round model.Setup.RoundToPlay) ]
              RoundSlider.slider model dispatch
              ResultTable.view (model: State) dispatch
              pre [] [str (sprintf "%A" model.State.ResolvedRounds)]
            ]

  div [ Id "main-container"; ClassName "columns"; ] [
    SettingsForm.view model dispatch
    div [ Id "results"; ClassName "column" ] [
        resultPanel
    ]
  ]
