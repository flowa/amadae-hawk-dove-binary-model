module Helpers

// If there is just one module
// moule information is omited from js
// this sucks if you want to add new modelue without
// restarting watchers hence we have here extra module
module GenerateModules =
    let value = true

module ArrayHelpers =
    let rand = System.Random()

    let swap (a: _[]) x y =
        let tmp = a.[x]
        a.[x] <- a.[y]
        a.[y] <- tmp

    // shuffle an array (in-place)
    let shuffle arr =
        arr
        |> Array.iteri (fun i _ -> swap arr i (rand.Next(i, Array.length arr)))
        arr

    let toPairs (items: 'a array) =
        seq {
            for pairIndex in 0 .. 2 .. items.Length - 1 ->
                items.[pairIndex], items.[pairIndex + 1]
        }
        |> Array.ofSeq

module ListHelpers =
    let shuffle l =
        l
        |> Array.ofList
        |> ArrayHelpers.shuffle
        |> Array.toList

    let toPairs (items: 'a list)=
        seq {
            for pairIndex in 0 .. 2 .. items.Length - 1 ->
                items.[pairIndex], items.[pairIndex + 1]
        }
        |> List.ofSeq


module Memoize =
    let memoize fn =
      let cache = new System.Collections.Generic.Dictionary<_,_>()
      (fun x ->
        match cache.TryGetValue x with
        | true, v -> v
        | false, _ -> let v = fn (x)
                      cache.Add(x,v)
                      v)