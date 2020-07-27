module Statistics

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
        member this.HawkPortion with get() = if this.TotalN = 0 then 0.0 else (float this.HawkN) / (float this.TotalN)
        member this.DovePortion with get() = if this.TotalN = 0 then 0.0 else 1.0 - this.HawkPortion

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

    let aggregateBy<'a when 'a : equality and 'a : comparison>
        (keyFn: ChallengeType * Strategy * Color -> 'a)
        (aggs: Map<ChallengeType * Strategy * Color, int>): Map<'a, int> =
        aggs
        |> Map.toList
        |> List.map (fun (key, total) -> (keyFn key), total)
        |> List.groupBy (fun (key, _) -> key)
        |> List.map (fun (key, subAggs) -> key, subAggs |> List.sumBy (fun (_, value) -> value))
        |> Map.ofList

    let valueOrZero<'a when 'a : equality and 'a : comparison> (map: Map<'a, int>) (key: 'a) =
        match map.TryFind(key) with
        | None -> 0
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


module Memoize =
    let memoize fn =
      let cache = new System.Collections.Generic.Dictionary<_,_>()
      (fun x ->
        match cache.TryGetValue x with
        | true, v -> v
        | false, _ -> let v = fn (x)
                      cache.Add(x,v)
                      v)

module ModelExtensions =
    open Model
    type GameRound with
        member this.Aggregates with get() = RoundStats.calcRoundAggregates(this)
        member this.StrategyStats () = RoundStats.strategyStats this.Aggregates
        member this.StrategyStatsFor (color: Color) = RoundStats.strategyStatsForColor this.Aggregates color
        member this.StrategyStatsFor (challengeType: ChallengeType) = RoundStats.strategyStatsForChallengeType this.Aggregates challengeType
        member this.StrategyStatsFor (challengeType: ChallengeType, color: Color) = RoundStats.strategyStatsForChallengeTypeAndColor this.Aggregates challengeType color
