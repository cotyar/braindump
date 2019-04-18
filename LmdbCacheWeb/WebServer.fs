namespace LmdbCacheWeb

open System.Reflection

module WebServer =
    open System.IO
    open Suave
    open Suave.Filters
    open Suave.Operators
    open System.Threading

    let StartWebServer (cts: CancellationToken) ip (port : uint32) =
        // Just in order to make homeFolder the same from both VS and direct "dotnet run"
        //let homeFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase.Replace("""file:\""", "")), "public") 
        let homeFolder = """C:\Work2\braindump\LmdbCacheWeb\app"""
        let config =
          { defaultConfig with 
                homeFolder = Some homeFolder
                cancellationToken = cts
                //logger =  
                bindings = [ HttpBinding.createSimple HTTP ip (int port) ] }
        let app : WebPart =
            choose [
                GET >=> path "/" >=> Files.file (Path.Combine(homeFolder, "build/index.html"))
                GET >=> Files.browseHome
                RequestErrors.NOT_FOUND "Page not found." 
            ]

        async {
            printfn "Starting Admin and Monitoring Web service started on host '%s' and port '%d'" ip port
            let (startUpdate, completion) = startWebServerAsync config app 
            do async {  let! startInfo = startUpdate
                        printfn "Admin and Monitoring Web service start info: '%A'" startInfo } |> Async.Start // Blocking on startUpdate directly will deadlock the webserver
            printfn "Started Admin and Monitoring Web service started on host '%s' and port '%d'" ip port
            do! completion
        } |> Async.StartAsTask
