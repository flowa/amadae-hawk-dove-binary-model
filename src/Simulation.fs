module Simulation

open Model
type StragyPropability =
    {
        Hawk: float
        Dove: float
    }
type ColorStatistics =
    {
        CountOfRedHawks: float
        CountOfBlueHawks: float
    }
    member this.HawkCountFor(c: Color) =
        match c with
        | Red  -> this.CountOfRedHawks
        | Blue -> this.CountOfRedHawks

type PropabilitiesForColor = Color * StragyPropability
type PropabilityMap = Map<Color, StragyPropability>

module NashEquilibrium =
    let calculateNashEquilibriumPortionOfHawksFromPayoff(payOff: PayoffMatrix) : float =
            let (hawkMax, doveMin) = payOff.[(Hawk, Dove)]
            let (doveMax, _)       = payOff.[(Dove, Dove)]
            let (hawkMin, _)       = payOff.[(Hawk, Hawk)]
            match (hawkMin - doveMin) with
            | 0.0 -> 1.0
            | _ ->
                let hawksPerDove = (doveMax - hawkMax) / (hawkMin - doveMin)
                let portionOfHawks = hawksPerDove / (1.0 + hawksPerDove)
                if (portionOfHawks > 1.0) then
                    1.0
                else
                    portionOfHawks

    let calculateNashEquilibriumPortionOfHawksFromPayoffAndPortionOfHawks
            (payOff: PayoffMatrix)
            (probabilityOfHawk: float)
            : float =
            let expectedValueForPayingHawk =
                match payOff.[(Hawk, Hawk)],  payOff.[(Hawk, Dove)] with
                | (whenOtherPlayedHawk, _), (whenOtherPlayedDove, _) ->
                    probabilityOfHawk * whenOtherPlayedHawk +
                    (1.0 - probabilityOfHawk) * whenOtherPlayedDove

            let expectedValueForPayingDove =
                match payOff.[(Dove, Hawk)],  payOff.[(Dove, Dove)] with
                | (whenOtherPlayedHawk, _), (whenOtherPlayedDove, _) ->
                    probabilityOfHawk * whenOtherPlayedHawk +
                    (1.0 - probabilityOfHawk)  * whenOtherPlayedDove

            match expectedValueForPayingDove with
            // If other is expected to play how I should play?
            //
            | 0.0 -> 1.0
            | _ ->
                // Peruming that everyone play alike in last round
                let expectedRatioOfHawks = expectedValueForPayingHawk / expectedValueForPayingDove
                let portionOfHawks = expectedRatioOfHawks / (1.0 + expectedRatioOfHawks)
                if (portionOfHawks > 1.0) then
                    1.0
                else
                    portionOfHawks


module Stats =
    let countOfAgentMatching (pairToMatch: Color * Strategy) (challenge: ResolvedChallenge) =
            challenge.ToColorChoiseList()
            |> List.filter ((=) pairToMatch)
            |> List.length
            |> float

    let calcStatsForChallenge (challenge: ResolvedChallenge) =
        {
            CountOfRedHawks = countOfAgentMatching (Red, Hawk) challenge
            CountOfBlueHawks = countOfAgentMatching (Blue, Hawk) challenge
        }


    let calcStatsForRound (round: GameRound): ColorStatistics =
        let total = round.Length * 2; // two player per round
        let initialValue = { CountOfBlueHawks = 0.0; CountOfRedHawks = 0.0 }
        let updateStats (accumulatedStats: ColorStatistics) (currentChallenge: ResolvedChallenge) =
            let currentChallengeStats = calcStatsForChallenge currentChallenge
            {
                CountOfRedHawks = accumulatedStats.CountOfRedHawks + currentChallengeStats.CountOfRedHawks
                CountOfBlueHawks = accumulatedStats.CountOfBlueHawks + currentChallengeStats.CountOfBlueHawks
            }

        round
        |> List.fold (updateStats) initialValue

    let calcStatsForLastRound (rounds: GameHistory): ColorStatistics =
        let lastRound = rounds |> List.rev |> List.head
        calcStatsForRound lastRound

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

let rand = System.Random()
module GameModes =
    let simpleGame (agent: Agent)
                   (opponentColor: Color)
                   (gameState: GameState)
                   : Strategy =
       agent.Strategy

    let nashEqlibiumGame (agent: Agent)
                   (opponentColor: Color)
                   (gameState: GameState)
                   : Strategy =
        let ``change of hawk`` =
            NashEquilibrium.calculateNashEquilibriumPortionOfHawksFromPayoff gameState.PayoffMatrix
        let chance = rand.NextDouble() // range [0.0, 1.0[
        if (``change of hawk`` > chance) then
            Hawk
        else
            Dove

    let nashEqlibiumOnBasedOfPayoffAndHistory (agent: Agent)
                   (opponentColor: Color)
                   (gameState: GameState) =
        let total =
            gameState.Agents
            |> List.filter (fun a -> a.Color = opponentColor)
            |> List.length
            |> float

        let hawkPortion =
            let stats = Stats.calcStatsForLastRound gameState.ResolvedRounds
            let countOfHawks = stats.HawkCountFor opponentColor
            countOfHawks / total

        let ``change of hawk`` =
            NashEquilibrium.calculateNashEquilibriumPortionOfHawksFromPayoffAndPortionOfHawks
               gameState.PayoffMatrix
               hawkPortion
        let chance = rand.NextDouble() // range [0.0, 1.0[
        printfn "hawkP %f change %f" ``change of hawk`` chance
        if (``change of hawk`` > chance) then
            Hawk
        else
            Dove

    let onHawksOnLastRound (agent: Agent)
                    (opponentColor: Color)
                    (gameState: GameState) =

        let total =
            gameState.Agents
            |> List.filter (fun a -> a.Color = opponentColor)
            |> List.length
            |> float

        let hawkPortion =
            let stats = Stats.calcStatsForLastRound gameState.ResolvedRounds
            let countOfHawks = stats.HawkCountFor opponentColor
            countOfHawks / total

        let chance = rand.NextDouble() // range [0.0, 1.0[
        if (hawkPortion> chance) then
            Hawk
        else
            Dove

    // let stage 3
    let stage2Game (agent: Agent)
                   (opponentColor: Color)
                   (gameState: GameState)
                   : Strategy =
        let probablities: PropabilityMap option =
            Stats.calcProbablities gameState

        match (probablities, opponentColor) with
        | (None, _) ->
            // if there are not stats play nashEquilibium game
            nashEqlibiumGame agent opponentColor gameState
        | (Some p, opponentColor) ->
            let matrix = gameState.PayoffMatrix
            let getPayoff = Stats.myPayoff matrix
            let pHawk = (p.Item opponentColor).Hawk
            let pDove = (p.Item opponentColor).Dove

            // Caclulate expected payoff for playinf hawk and for playing dove
            let euHawk = pHawk * getPayoff (Hawk, Hawk) +
                         pDove * getPayoff (Hawk, Dove)
            let euDove = pHawk * getPayoff (Dove, Hawk) +
                         pDove * getPayoff (Dove, Dove)

            match (agent.Color = opponentColor), (euHawk - euDove) with
            | true , _ -> onHawksOnLastRound agent opponentColor gameState
            // When you have expected value for playing hawk and playing dove are equal
            // choose randomly
            | false , 0.0 ->
                let rand = System.Random()
                let change = rand.NextDouble() // Range [0.0, 1.0]
                if change < 0.5 then
                    Dove
                else
                    Hawk
            // if expected payoff for playing hawk is better, play hawk
            // otherwise play dove
            | false, diff when diff > 0.0 ->
                Hawk
            | _  ->
                Dove
