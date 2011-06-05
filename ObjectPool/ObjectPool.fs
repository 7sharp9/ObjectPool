module Poc
open System.Collections.Concurrent

//Agent alias for MailboxProcessor
type Agent<'T> = MailboxProcessor<'T>

///One of three messages for our Object Pool agent
type PoolMessage<'a> =
    | Get of AsyncReplyChannel<'a>
    | Put of 'a * AsyncReplyChannel<unit>
    | Clear of AsyncReplyChannel<List<'a>>

/// Object pool representing a reusable pool of objects
type ObjectPool<'a>(generate: unit -> 'a, initialPoolCount) = 
    let pool = List.init initialPoolCount (fun (x) -> generate()) |> ref
    let agent = Agent.Start(fun inbox ->
        let rec loop(x) = async {
            let! msg = inbox.Receive()
            match msg with
            | Get(reply) -> 
                match !pool with
                | a :: b -> 
                    pool:= b
                    reply.Reply(a)
                | [] -> reply.Reply(generate())
                return! loop(x-1)
            | Put(value, reply)-> 
                pool:=  value :: !pool
                reply.Reply()
                return! loop(x+1) 
            | Clear(reply) -> 
                reply.Reply(!pool)
                pool := List.empty<'a> 
                return! loop(0)            
        }
        loop(0))

    /// Clears the object pool, returning all of the data that was in the pool.
    member this.ToListAndClear() = 
        agent.PostAndAsyncReply(Clear)
    /// Puts an item into the pool
    member this.Put(item) = 
        agent.PostAndAsyncReply((fun ch -> Put(item, ch)))
    /// Gets an item from the pool or if there are none present use the generator
    member this.Get(item) = 
        agent.PostAndAsyncReply(Get)