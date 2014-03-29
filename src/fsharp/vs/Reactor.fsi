namespace Microsoft.FSharp.Compiler.SourceCodeServices

/// Reactor is intended for long-running but interruptible operations, interleaved
/// with one-off asynchronous operations. 
///
/// It is used to guard the global compiler state while maintaining  responsiveness on 
/// the UI thread.
module internal Reactor = 
    
    /// Does one unit of work and returns true if there is more work to do.
    type BuildStepper = unit -> (* keep building *) bool
    
    /// A synchronous or asynchronous operation to perform
    type Operation = unit -> unit

    /// Reactor operations
    [<Sealed>]
    type Reactor =
        /// Start building. The build function will return true if there is more work to do.
        member StartBuilding : build:BuildStepper -> unit
        /// Halt the current build.
        member StopBuilding : unit -> unit
        /// Block until the current build is complete.
        member WaitForBackgroundCompile : unit -> unit
        /// Start an operation and return immediately. Restart the most recent build after the operation is complete.
        member StartAsyncOp : op:Operation -> unit
    
    // TODO: For all AsyncOps: if the operation gets cancelled, the background thread and Reactor don't abandon their work,
    // even when it is ultimately an Eventually<_> compuation which could easily be abandoned, or an IncrementalBuild.Eval
    // operation which can be halted part way through.

        /// Start an operation and return an async handle to its result. 
        member RunAsyncOp : (unit -> 'T) -> Async<'T>

    /// Get the reactor for FSharp.Compiler.dll
    val Reactor : unit -> Reactor
  
