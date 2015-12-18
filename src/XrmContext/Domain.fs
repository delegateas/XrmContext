namespace DG.XrmContext

open System
open Microsoft.Xrm.Sdk.Client

type XrmAuthentication = {
  url: Uri
  username: string
  password: string
  domain: string option
  ap: AuthenticationProviderType option
}

type XrmContextSettings = {
  out: string option
  ns: string option
  context: string option
  entities: string[] option
  solutions: string[] option
  deprecatedPrefix: string option
}