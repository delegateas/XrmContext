module internal DG.XrmContext.IntermediateRepresentation

open System


type XrmOptionSetType = Picklist | State | Status | Boolean

type XrmOptionLocalized = {  
  displayName: string
  description: string
}

type XrmOption = {
  value: int
  label: string
  index: int  
  color: string
  localization: Map<int,XrmOptionLocalized>
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
  | OptionSetCollection of string
  | PartyList

type XrmAttribute = {
  schemaName: string
  logicalName: string
  displayName: string option
  varType: XrmAttributeType
  maxLength: int option
  minValue: int option
  maxValue: int option
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
  typecode: int option
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
  isIntersect: bool
}

type XrmIntersect = string * XrmAttribute list

type InterpretedState = {
  outputDir: string
  entities: XrmEntity[]
  ns: string
  context: string option
  intersections: XrmIntersect[]
}
