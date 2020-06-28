module Simulation

open Model
type StragyPropability =
    {
        Hawk: float
        Dove: float
    }
type PropabilitiesForColor = Color * StragyPropability
type PropabilityMap = Map<Color, StragyPropability>

module Stats =
    let calcStrategyPropability
        (color: Color, choises: Strategy list)
        : PropabilitiesForColor =
        let total = (float choises.Length)
        let countFor choice =
            choises
            |> List.filter (fun item -> item = choice)
            |> List.length
            |> float

        color, {
           Hawk = (countFor Hawk) / total
           Dove = (countFor Dove) / total
        }
    let mapToColorChoicePairs
        (challenge: ResolvedChallenge):
        (Color * Strategy) list =
        match challenge with
        | { ResolvedChallenge.Players = (p1, p2)
            Choices = (c1, c2)} ->
                [
                    (p1.Color, c1)
                    (p2.Color, c2)
                ]

    let selectStartegyFromPair
        (pairs: (Color * Strategy) list )
        : Strategy list =
        pairs
        |> List.map (fun (c, s) -> s)
    let mapToColorStrategyListPair
        (color: Color, pairs) =
        (color, selectStartegyFromPair pairs)
    let calcProbablities
        (gameState: GameState)
        : PropabilityMap option =
                match gameState.ResolvedRounds with
                | [] -> None
                | rounds ->
                    let lastRound = rounds |> List.last
                    lastRound
                    |> List.collect mapToColorChoicePairs
                    |> List.groupBy (fun (color, _) -> color)
                    |> List.map (mapToColorStrategyListPair
                                >> calcStrategyPropability)
                    |> Map.ofList
                    |> Some

    let myPayoff (matrix: PayoffMatrix) pair =
        let (myPayoff, _) = matrix.[pair]
        myPayoff

module GameModes =
    // Stage 1 & 2?
    let simpleGame (agent: Agent)
                   (opponentColor: Color)
                   (gameState: GameState)
                   : Strategy =
       agent.Strategy

    let nashEqlibiumGame (agent: Agent)
                   (opponentColor: Color)
                   (gameState: GameState)
                   : Strategy =
       // see page 15, fig 5
       // Random on based on nashEwulilibrium
       //
       agent.Strategy

    // let stage 3
    let stage2Game (agent: Agent)
                   (opponentColor: Color)
                   (gameState: GameState)
                   : Strategy =
        let probablities: PropabilityMap option =
            Stats.calcProbablities gameState

        match (probablities, opponentColor) with
        | (None, _) ->
            // is this correct
            simpleGame agent opponentColor gameState
        | (Some p, opponentColor) ->
            let matrix = gameState.PayoffMatrix
            let getPayoff = Stats.myPayoff matrix
            let pHawk = (p.Item opponentColor).Hawk
            let pDove = (p.Item opponentColor).Dove

            // Now we just calk expected value on basis
            // of opponent color, not on my and opponent color
            // Is this correct
            let euHawk = pHawk * getPayoff (Hawk, Hawk) +
                         pDove * getPayoff (Hawk, Dove)
            let euDove = pHawk * getPayoff (Dove, Hawk) +
                         pDove * getPayoff (Dove, Dove)

            // What should be done if propabilities are equal

            match (agent.Color = opponentColor), (euHawk - euDove) with
            // When expected values are equal what should be chosen
            | true , _ -> simpleGame agent opponentColor gameState
            | false , 0.0 ->
                let rand = System.Random();
                if (rand.Next(1,2) = 1) then
                    Dove
                else
                    Hawk
            | false, diff when diff > 0.0 ->
                Hawk
            | _  ->
                Dove
