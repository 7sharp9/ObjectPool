open Poc
open System

type Customer = 
    {First : string; Last : string; AccountNumber : int;}
    override m.ToString() = sprintf "%s %s, Acc: %d" m.First  m.Last m.AccountNumber

let names = ["John"; "Paul"; "George"; "Ringo"]
let lastnames = ["Lennon";"McCartney";"Harison";"Starr";]
let rand = System.Random()

let randomFromList list= 
    let length = List.length list
    let skip = rand.Next(0, length)
    list |> List.toSeq |> (Seq.skip skip ) |> Seq.head

let customerGenerator() =
    Async.RunSynchronously(Async.Sleep(100))
    { First = names |> randomFromList; 
      Last= lastnames |> randomFromList; 
      AccountNumber = rand.Next(100000, 999999);}
  
let numberToGenerate = 10    

let objectPool = ObjectPool(customerGenerator, numberToGenerate)

printfn "%d customers in pool" numberToGenerate

let numberToRun = seq { 0 .. numberToGenerate * 2 - 1 }

let sw = System.Diagnostics.Stopwatch.StartNew()

for x in numberToRun do 
    (   sw.Start()   
        let result = Async.RunSynchronously( async{return! objectPool.Get()})
        do sw.Stop()
        printfn "*%d %O, Generation time %A ms" x result sw.Elapsed.TotalMilliseconds
        sw.Reset()
    )

printfn "Press any key to exit."
Console.ReadKey() |> ignore

