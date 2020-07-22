module MainView

open Model
open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props


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

module ResultTable =
  let agentBox (rounds: int) (model: Agent) =
    let avg =
      match rounds with
      | 0 -> 0.0
      | r -> model.Payoff / (float r)
    div [ ClassName (sprintf "agent-box %A %A" model.Color model.Strategy)] [
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
  div [ Id "main-container"; ClassName "columns"; ] [
    div [ ClassName "column is-one-quarter"; Id "settings" ] [
      h1 [] [ str "Setting" ]
      pre [] [
        str (sprintf "%A" model.Setup)
      ]
    ]
    div [ Id "results"; ClassName "column" ] [
      h1 [] [ str (sprintf "Results (Round %i/%i)" model.CurrentRound model.Setup.RoundToPlay) ]
      RoundSlider.slider model dispatch
      ResultTable.view (model: State) dispatch
      pre [] [str (sprintf "%A" model.State.ResolvedRounds)]

    ]
  ]
