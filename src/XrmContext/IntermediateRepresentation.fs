module internal DG.XrmContext.IntermediateRepresentation

open System


type XrmOptionSetType = Picklist | State | Status | Boolean

type XrmOption = {
  label: string 
  value: int
}

type XrmOptionSet = {
  logicalName: string
  displayName: string
  osType: XrmOptionSetType
  options: XrmOption[]
  isGlobal: bool
}

type XrmAttributeType = 
  | Default of Type 
  | OptionSet of string
  | PartyList

type XrmAttribute = {
  schemaName: string
  logicalName: string
  varType: XrmAttributeType
  canSet: bool
  canGet: bool
  description: string list option
  isDeprecated: bool
}

type XrmRelationship = {
  varName: string
  schemaName: string
  attributeName: string
  relatedEntity: string
  referencing: bool
  useEntityRole: bool
}

type XrmAlternateKey = {
  schemaName: string
  displayName: string
  keyAttributes: (string * string * XrmAttributeType)[]
}

type XrmEntity = {
  typecode: int
  schemaName: string
  logicalName: string
  description: string list option
  primaryNameAttribute: string
  primaryIdAttribute: string
  stateAttribute: XrmOptionSet option
  statusAttribute: XrmOptionSet option
  attr_vars: XrmAttribute list 
  rel_vars: XrmRelationship list
  opt_sets: XrmOptionSet list
  alt_keys: XrmAlternateKey list
  interfaces: string list option
}

type XrmIntersect = string * XrmAttribute list

type InterpretedState = {
  outputDir: string
  entities: XrmEntity[]
  ns: string
  context: string option
  intersections: XrmIntersect[]
}
