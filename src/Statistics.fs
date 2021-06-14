module Statistics
open System.Collections.Generic

module RoundStats =
    open Model

    type StrategyStats =
        | HakwDoveStats of hawkN: int * doveN: int
        member this.HawkN with get() =
            match this with
            | HakwDoveStats (n, _) -> n
        member this.DoveN with get() =
            match this with
            | HakwDoveStats (_, n) -> n
        member this.TotalN with get() = this.HawkN + this.DoveN
        // when showing stats it's possible that there are segments where total is 0, portion when total is 0 is 0 for hawks and doves
        member this.HawkPortion with get() = if this.TotalN = 0 then 0.0m else (decimal this.HawkN) / (decimal this.TotalN)
        member this.DovePortion with get() = if this.TotalN = 0 then 0.0m else 1.0m - this.HawkPortion

    let calcRoundAggregates (round: GameRound): Map<ChallengeType * Strategy * Color, int> =
        let items = round.ToList()
        items
        |>  List.collect (
            fun challenge ->
                let (a1, a2) = challenge.Players
                let challengeType = challenge.ChalengeType
                [
                    (challengeType, a1.Strategy.Value, a1.Color)
                    (challengeType, a2.Strategy.Value, a2.Color)
                ]
            )
        |> List.groupBy id
        |> List.map (fun (key, items) -> (key, items.Length))
        |> Map.ofList

    let filterOnlyAgentOwnMoves (agent: Agent) (round: GameRound)  =
        round.ToList()
        |> List.filter (fun challenge ->
            match challenge with
            | { Players = (player1, player2) } -> agent.Id = player1.Id || agent.Id = player2.Id
        )
        |> List.head

    let selectTheOtherAgentStatsTuple (agent: Agent) (challenge: ResolvedChallenge) =
        let (a1, a2) = challenge.Players
        let challengeType = challenge.ChalengeType
        if (a1.Id = agent.Id) then
            (challengeType, a2.Strategy.Value, a2.Color)
        else
            (challengeType, a1.Strategy.Value, a1.Color)

    let rec calcRoundAggregatesForAgentsWithCacheRecursive
        (cache: AgentViewCache)
        (roundIndex: int, agent: Agent, history: GameHistory): Map<ChallengeType * Strategy * Color, int> =
            let calcForRound (round: GameRound) =
                let key = (filterOnlyAgentOwnMoves agent >> selectTheOtherAgentStatsTuple agent) round
                Map.ofList [key, 1]
            match cache.ContainsKey(roundIndex, agent.Id), roundIndex with
            | true, _ ->
                cache.[roundIndex, agent.Id]
            | false, 0 ->
                let firstRound = calcForRound (history.Unwrap().[roundIndex])
                cache.Add((roundIndex, agent.Id), firstRound)
                firstRound
            | false, round ->
                let previousRoundIndex = round - 1
                // Get previous round stats recurivelu
                // (tail recursion would be better, but there will never be milloins or round so its not needed)
                let previousRound = calcRoundAggregatesForAgentsWithCacheRecursive cache (previousRoundIndex, agent, history)
                let thisRound = calcForRound (history.Unwrap().[roundIndex])
                let selectKey (key, _) = key
                let selectValue (_, value) = value
                let currentRoundStats =
                    List.concat [
                        (previousRound |> Map.toList)
                        (thisRound |> Map.toList)
                    ]
                    |> List.groupBy selectKey
                    |> List.map (fun (key, items) -> (key, items |> List.sumBy selectValue))
                    |> Map.ofList
                cache.Add((roundIndex, agent.Id), currentRoundStats)
                currentRoundStats

    let rec calcRoundAggregatesForAgentsWithCache cache (agent: Agent, history: GameHistory) =
        let lastRountIndex = history.TotalRounds - 1
        calcRoundAggregatesForAgentsWithCacheRecursive cache (lastRountIndex, agent, history)

    let calcRoundAggregatesForAgents(agent: Agent, history: GameHistory): Map<ChallengeType * Strategy * Color, int> =
        history.Unwrap()
        |> Array.map (filterOnlyAgentOwnMoves agent >> selectTheOtherAgentStatsTuple agent)
        |> Array.groupBy id
        |> Array.map (fun (key, items) -> (key, items.Length))
        |> Map.ofArray

    let aggregateBy<'a when 'a : equality and 'a : comparison>
        (keyFn: ChallengeType * Strategy * Color -> 'a)
        (aggs: Map<ChallengeType * Strategy * Color, int>): Map<'a, int> =
        aggs
        |> Map.toList
        |> List.map (fun (key, total) -> (keyFn key), total)
        |> List.groupBy (fun (key, _) -> key)
        |> List.map (fun (key, subAggs) -> key, subAggs |> List.sumBy (fun (_, value) -> value))
        |> Map.ofList

    let valueOrZero<'key, 'value when 'key : equality and 'key : comparison> (map: Map<'key, 'value>) (key: 'key) =
        match map.TryFind(key) with
        | None -> Unchecked.defaultof<'value>
        | Some v -> v


    let strategyStatsForChallengeTypeAndColor roundAggs (challengeType: ChallengeType) (color: Color) =
        let hawkN = valueOrZero roundAggs (challengeType, Hawk, color)
        let doveN = valueOrZero roundAggs (challengeType, Dove, color)
        HakwDoveStats (hawkN, doveN)

    let strategyStatsForChallengeType roundAggs (challengeType: ChallengeType) =
        let subAgg = aggregateBy (fun (challenge, strategy, _) -> (challenge, strategy)) roundAggs
        let hawkN = valueOrZero subAgg (challengeType, Hawk)
        let doveN = valueOrZero subAgg (challengeType, Dove)
        HakwDoveStats (hawkN, doveN)

    let strategyStatsForColor roundAggs (color: Color) =
        let subAgg = aggregateBy (fun (_, strategy, color) -> (strategy, color)) roundAggs
        let hawkN = valueOrZero subAgg (Hawk, color)
        let doveN = valueOrZero subAgg (Dove, color)
        HakwDoveStats (hawkN, doveN)

    let strategyStats roundAggs =
        let subAgg = aggregateBy (fun (_, strategy, _) -> strategy) roundAggs
        let hawkN = valueOrZero subAgg Hawk
        let doveN = valueOrZero subAgg Dove
        HakwDoveStats (hawkN, doveN)

module ModelExtensions =
    open Model
    type HistoryStatisticsView with
        member this.Aggregates (agent: Agent) =
            RoundStats.calcRoundAggregatesForAgentsWithCache this.AgentViewCache (agent, this.History)
        member this.StrategyStats (agent: Agent) = RoundStats.strategyStats (this.Aggregates agent)
        member this.StrategyStatsFor (agent: Agent, color: Color) = RoundStats.strategyStatsForColor (this.Aggregates agent) color
        member this.StrategyStatsFor (agent: Agent, challengeType: ChallengeType) = RoundStats.strategyStatsForChallengeType (this.Aggregates agent) challengeType
        member this.StrategyStatsFor (agent: Agent, challengeType: ChallengeType, color: Color) = RoundStats.strategyStatsForChallengeTypeAndColor (this.Aggregates agent) challengeType color

    type GameRound with
        member this.Aggregates with get() = RoundStats.calcRoundAggregates(this)
        member this.StrategyStats () = RoundStats.strategyStats this.Aggregates
        member this.StrategyStatsFor (color: Color) = RoundStats.strategyStatsForColor this.Aggregates color
        member this.StrategyStatsFor (challengeType: ChallengeType) = RoundStats.strategyStatsForChallengeType this.Aggregates challengeType
        member this.StrategyStatsFor (challengeType: ChallengeType, color: Color) = RoundStats.strategyStatsForChallengeTypeAndColor this.Aggregates challengeType color
       
        member this.FullyDominatingColor
            with get() =
                let red = this.StrategyStatsFor(DifferentColor, Red)
                let blue = this.StrategyStatsFor(DifferentColor, Blue)
                let both = this.StrategyStatsFor(DifferentColor)            
                match (red.HawkN, blue.HawkN, both.HawkN) with
                | 0, _, all when all > 0 -> Some Blue
                | _, 0, all when all > 0 -> Some Red
                | _ -> None
         member this.InDifferentColorEncountersColorsHaveSeparated
            with get() =
                this.FullyDominatingColor.IsSome
    
            
        
        member this.PayoffAccumulativeAvgForRedAndBlue
            with get() =
                let breakdown =
                    this.Agents
                    |> List.groupBy (fun agent -> agent.Color)
                    |> List.map (fun (color, agents) ->
                        color, agents |> List.averageBy (fun a -> a.Payoff))
                    |> Map.ofList
                let avgAll =
                    this.Agents
                    |> List.averageBy (fun a -> a.Payoff)

                (RoundStats.valueOrZero breakdown Red), (RoundStats.valueOrZero breakdown Blue), avgAll

    type GameHistory with
        member this.Aggregates (agent: Agent)  = RoundStats.calcRoundAggregatesForAgents(agent, this)
        member this.StrategyStats (agent: Agent) = RoundStats.strategyStats (this.Aggregates agent)
        member this.StrategyStatsFor (agent: Agent, color: Color) = RoundStats.strategyStatsForColor (this.Aggregates agent) color
        member this.StrategyStatsFor (agent: Agent, challengeType: ChallengeType) = RoundStats.strategyStatsForChallengeType (this.Aggregates agent) challengeType
        member this.StrategyStatsFor (agent: Agent, challengeType: ChallengeType, color: Color) = RoundStats.strategyStatsForChallengeTypeAndColor (this.Aggregates agent) challengeType color
        member this.FirstRoundWithNConsecutiveRoundOfSeparatedColors (requiredRoundCount: int) =
                this.Unwrap()
                |> Array.mapi (fun index round -> (index + 1), round)
                |> Array.windowed requiredRoundCount
                |> Array.filter (fun window ->
                    window
                    |> Array.forall (fun (_, round) -> round.InDifferentColorEncountersColorsHaveSeparated))
                |> Array.map (fun window -> window |> Array.last)
                |> Array.map (fun (roundNumber, _) -> roundNumber)
                |> Array.tryHead

        member this.DominatingColorAfterSeparation (requiredConsecutiveSeparatedRounds: int) =
            this.FirstRoundWithNConsecutiveRoundOfSeparatedColors requiredConsecutiveSeparatedRounds  
            |> Option.map this.GetRoundByRoundNumber
            |> Option.bind (fun round -> round.FullyDominatingColor)