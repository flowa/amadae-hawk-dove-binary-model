module Statistics

module RoundStats =
    open Model

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

module ModelExtensions =
    open Model
    type GameRound with
        member this.Aggregates with get() = RoundStats.calcRoundAggregates(this)