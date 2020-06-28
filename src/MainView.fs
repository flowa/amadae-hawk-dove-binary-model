module MainView

open Model
open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props

// VIEW (rendered with React)

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
                ofFloat avg
              ]
          ]
      div [ ClassName "agent-id"] [ str "#"; ofInt model.Id ]
    ]

  let view (model: State) dispatch =
    let playedRounds = model.State.ResolvedRounds.Length
    let agents = (model.State.Agents |> List.map (agentBox playedRounds))
    div [ ClassName "agent-listing" ] agents

// Main view
let view (model: State) dispatch =
  div [ Id "main-container"; ClassName "columns"; ] [
    div [ ClassName "column is-one-quarter"; Id "settings" ] [
      h1 [] [ str "Setting" ]
      pre [] [
        str (sprintf "%A" model.Setup)
      ]
    ]
    div [ Id "results"; ClassName "column" ] [
      h1 [] [ str (sprintf "Results (Round %i)" model.State.ResolvedRounds.Length) ]
      ResultTable.view (model: State) dispatch
      pre [] [str (sprintf "%A" model.State.ResolvedRounds)]

    ]
  ]
