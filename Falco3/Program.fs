open System.Threading.Tasks
open Falco
open Falco.Routing
open Falco.HostBuilder

type AsyncService<'input, 'success, 'error> = 'input -> Task<Result<'success, 'error>>

let runAsync
    (serviceHandler: AsyncService<'input, 'success, 'error>)
    (handleOk: 'success -> HttpHandler)
    (handleError: 'error -> HttpHandler)
    (input: 'input)
    : HttpHandler =
    fun ctx ->
        task {
            let! response = serviceHandler input

            let respondWith: HttpHandler =
                match response with
                | Ok output -> handleOk output
                | Error error -> handleError error

            do! respondWith ctx
        }

type User = { Username: string; IsAdmin: bool }
type LoginRequest = { Username: string; Password: string }

type LoginError = | InvalidUsernameAndOrPassword

let workflow (input: LoginRequest) =
    task {
        // simulate db call and password hashing.
        do! Task.Delay(130)

        if input.Username = "admin" && input.Password = "password" then
            return
                Ok
                    { Username = input.Username
                      IsAdmin = true }
        else
            return Error InvalidUsernameAndOrPassword
    }

let handler: HttpHandler =
    let handleSuccess (_: User) : HttpHandler = Response.ofJson {| success = true |}
    let handleFailure (_: LoginError) : HttpHandler = Response.ofJson {| success = false |}
    Request.mapJson (runAsync workflow handleSuccess handleFailure)

webHost [||] { endpoints [ post "/login" handler ] }
