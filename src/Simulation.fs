module Simulation

open Model
module GameModes =
    let simpleGame (agent: Agent)
                   (opponentColor: Color)
                   (history: GameHistory): Strategy =
       agent.Strategy
