namespace DG.XrmContext

open System
open Microsoft.Xrm.Sdk.Client
open Microsoft.Xrm.Sdk.Metadata
open System.Runtime.Serialization

type Version = int * int * int * int
type EntityIntersect = string * string[]

type XrmAuthentication = {
  url: Uri
  username: string
  password: string
  domain: string option
  ap: AuthenticationProviderType option
}

type XcGenerationSettings = {
  out: string option
  ns: string option
  context: string option
  deprecatedPrefix: string option
  sdkVersion: Version option
  intersections: EntityIntersect[] option
  labelMapping: (string * string)[] option
  oneFile: bool
}

type XcRetrievalSettings = {
  entities: string[] option
  solutions: string[] option
}



/// Serializable record containing necessary (meta)data
[<DataContract>]
type RawState = {

  [<field : DataMember>]
  crmVersion: Version

  [<field : DataMember>]
  metadata: EntityMetadata[]
}
