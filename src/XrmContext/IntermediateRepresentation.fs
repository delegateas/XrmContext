namespace DG.XrmContext

open System

module internal IntermediateRepresentation =

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

  type XrmEntity = {
    typecode: int
    schemaName: string
    logicalName: string
    primaryNameAttribute: string
    primaryIdAttribute: string
    stateAttribute: XrmOptionSet option
    statusAttribute: XrmOptionSet option
    attr_vars: XrmAttribute list 
    rel_vars: XrmRelationship list
    opt_sets: XrmOptionSet list
    }




