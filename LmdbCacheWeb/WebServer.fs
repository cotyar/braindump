namespace LmdbCacheWeb

module WebServer =
    open System.IO
    open Suave
    open Suave.Filters
    open Suave.Operators
    open System.Threading

    let StartWebServer (cts: CancellationToken) ip port =
        let config =
          { defaultConfig with 
                homeFolder = Some (Path.GetFullPath "./public")
                cancellationToken = cts
                //logger =  
                bindings = [ HttpBinding.createSimple HTTP ip port ] }
        let app : WebPart =
            choose [
                //GET >=> path "/" >=> Successful.OK "101010" 
                GET >=> path "/" >=> Files.file (Path.GetFullPath "./public/index.html")
                GET >=> Files.browseHome
                RequestErrors.NOT_FOUND "Page not found." 
            ]

        async {
            printfn "Starting Admin and Monitoring Web service started on host '%s' and port '%d'" ip port
            let (waitStart, completion) = startWebServerAsync config app 
            //let! startInfo = waitStart
            printfn "Started Admin and Monitoring Web service started on host '%s' and port '%d'" ip port
            //printfn "Admin and Monitoring Web service start info: '%A'" startInfo
            do! completion
        } |> Async.StartAsTask
