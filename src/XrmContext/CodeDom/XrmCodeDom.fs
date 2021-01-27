module internal DG.XrmContext.XrmCodeDom

open System
open System.Linq
open System.CodeDom
open System.Diagnostics
open System.Collections.Generic
open System.Runtime.Serialization

open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Metadata
open Microsoft.Xrm.Sdk.Client

  
open Utility
open IntermediateRepresentation
open CodeDomHelper
open System.ComponentModel

let getVarDefFromAttribute attribute =
  match attribute.varType with
  | PartyList ->
    "EntityCollection",
    CodeTypeReference "ActivityParty" |> Some,
    CodeTypeReference(typedefof<IEnumerable<_>>.Name, CodeTypeReference "ActivityParty")

  | OptionSet enumName -> 
    "OptionSetValue", 
    CodeTypeReference enumName |> Some,
    CodeTypeReference (sprintf "%s?" enumName)

   | OptionSetCollection enumName ->
    "OptionSetCollectionValue", 
    CodeTypeReference enumName |> Some,
    CodeTypeReference(typedefof<IEnumerable<_>>.Name, CodeTypeReference enumName)

  | Default ty when ty.Equals(typeof<Money>) ->
    "MoneyValue", 
    None,
    CodeTypeReference("decimal?")

  | Default ty -> 
    let varType = getSafeAttributeTypeRef ty
    sprintf "AttributeValue", Some varType, varType


(** Entity Attribute *)
let MakeEntityAttribute usedProps (attribute:XrmAttribute) =
  let funcName, varType, returnType = getVarDefFromAttribute attribute
    
  let name = CrmNameHelper.attributeMap.TryFind attribute.logicalName ?| attribute.schemaName
  let validName = getValidName usedProps name
  let updatedUsedProps = usedProps.Add validName
  let var = Variable validName returnType
  var.CustomAttributes.Add(EntityAttributeLogicalNameAttribute attribute.logicalName) |> ignore
  if attribute.displayName.IsSome then
    var.CustomAttributes.Add(CodeDomHelper.EntityAttributeCustomAttribute(AttributeName typeof<DisplayNameAttribute>, attribute.displayName.Value)) |> ignore
  if attribute.maxLength.IsSome then
    var.CustomAttributes.Add(CodeDomHelper.EntityAttributeCustomAttribute("MaxLength", attribute.maxLength.Value)) |> ignore
  if attribute.minValue.IsSome && attribute.minValue.IsSome then
    var.CustomAttributes.Add(
      CodeDomHelper.EntityAttributeCustomAttribute("Range", Option.get attribute.minValue, Option.get attribute.maxValue)) |> ignore

  // Comment summary
  match attribute.description with
  | Some desc -> var.Comments.AddRange(CommentSummary desc)
  | None -> ()

  // Getter
  if attribute.canGet then 
    var.HasGet <- true
    var.GetStatements.Add(
      Return(MethodInvoke ("Get" + funcName) varType 
        [|StringLiteral attribute.logicalName|])) |> ignore

  // Setter
  if attribute.canSet then
    var.HasSet <- true
    var.SetStatements.Add(
      MethodInvoke ("Set" + funcName) 
        None [|StringLiteral attribute.logicalName; VarRef "value"|]) |> ignore

    
  // Deprecated attribute check
  if attribute.isDeprecated then
    var.CustomAttributes.Add(CodeAttributeDeclaration(typeof<ObsoleteAttribute>.Name)) |> ignore

  updatedUsedProps, var :> CodeTypeMember
  

(** Interface Attribute *)
let MakeInterfaceAttribute usedProps (attribute: XrmAttribute) =
  let funcName, varType, returnType = getVarDefFromAttribute attribute
    
  let name = CrmNameHelper.attributeMap.TryFind attribute.logicalName ?| attribute.schemaName
  let var = Variable name returnType

  var.HasGet <- attribute.canGet
  var.HasSet <- attribute.canSet
    
  // Deprecated attribute check
  if attribute.isDeprecated then
    var.CustomAttributes.Add(CodeAttributeDeclaration(typeof<ObsoleteAttribute>.Name)) |> ignore

  var :> CodeTypeMember


(** Entity Relationship *)
let MakeEntityRelationship (usedProps:Set<string>) (relationship:XrmRelationship) =
  let entityType = relationship.relatedEntity |> CodeTypeReference

  let baseType, funcName =
    match relationship.referencing with
    | true  -> entityType, "Entity"
    | false -> 
      sprintf "IEnumerable<%s>" relationship.relatedEntity |> CodeTypeReference, 
      "Entities"
      
  let validName = getValidName usedProps relationship.varName
  let updatedUsedProps = usedProps.Add validName
  let var = Variable validName baseType

    
  let entityRole = 
    match relationship.useEntityRole, relationship.referencing with
    | false, _    -> CodePrimitiveExpression(null) :> CodeExpression
    | true, true  -> FieldRef typeof<EntityRole>.Name "Referencing" :> CodeExpression
    | true, false -> FieldRef typeof<EntityRole>.Name "Referenced" :> CodeExpression


  if relationship.referencing then
    var.CustomAttributes.Add(EntityAttributeLogicalNameAttribute relationship.attributeName) |> ignore

  if relationship.useEntityRole then
    var.CustomAttributes.Add(RelationshipCustomAttribute relationship.schemaName (Some entityRole)) |> ignore
  else
    var.CustomAttributes.Add(RelationshipCustomAttribute relationship.schemaName None) |> ignore
      

  // Getter
  var.HasGet <- true
  var.GetStatements.Add(
    Return(MethodInvoke ("GetRelated" + funcName) (Some entityType) 
      [|StringLiteral relationship.schemaName; entityRole|])) |> ignore

  // Setter
  var.HasSet <- true
  var.SetStatements.Add(
    MethodInvoke ("SetRelated" + funcName) None 
      [|StringLiteral relationship.schemaName; entityRole; VarRef "value"|]) |> ignore

  updatedUsedProps, var :> CodeTypeMember
 

(** Entity Alternate Keys *)
let MakeEntityAltKey usedProps (altKey:XrmAlternateKey) =
    
  let name = sprintf "AltKey_%s" altKey.schemaName
  let validName = getValidName usedProps name
  let updatedUsedProps = usedProps.Add validName
  let func = Function validName (VoidType())

  func.Statements.Add(MemberMethodInvoke "KeyAttributes" "Clear" None [||]) |> ignore
  altKey.keyAttributes
  |> Array.iter (fun (logicalName, schemaName, ty) -> 
    func.Parameters.Add(
      CodeParameterDeclarationExpression(getAltKeyVarType ty, schemaName)) |> ignore
    func.Statements.Add(
      MemberMethodInvoke "KeyAttributes" "Add" None 
        [| StringLiteral(logicalName); VarRef schemaName |]) |> ignore
  )

  func.Comments.AddRange(
    CommentSummary [sprintf "Set values for the alternate key called '%s'" altKey.displayName])
      
  updatedUsedProps, func :> CodeTypeMember

(** Entity Static Retrieve Method *)
let MakeEntityStaticRetrieve (entityName:string) =
    
  let name = "Retrieve"
  let func = Function name (TypeRef entityName)
  func.Attributes <- func.Attributes ||| MemberAttributes.Static

  func.Parameters.Add(CodeParameterDeclarationExpression(typeof<IOrganizationService>.Name, "service")) |> ignore
  func.Parameters.Add(CodeParameterDeclarationExpression(typeof<Guid>.Name, "id")) |> ignore
  func.Parameters.Add(CodeParameterDeclarationExpression(sprintf "params Expression<Func<%s,object>>[]" entityName, "attrs")) |> ignore
  func.Statements.Add(Return <| MemberMethodInvoke "service" "Retrieve" None 
    [| VarRef "id"; VarRef "attrs" |]) |> ignore

  func :> CodeTypeMember

(** Entity Alternate Keys Static Retrieve Methods *)
let MakeEntityAltKeyRetrieve (entityName:string) (altKey:XrmAlternateKey) =
    
  let name = sprintf "Retrieve_%s" altKey.schemaName
  let func = Function name (TypeRef entityName)
  func.Attributes <- func.Attributes ||| MemberAttributes.Static

  func.Parameters.Add(CodeParameterDeclarationExpression(typeof<IOrganizationService>.Name, "service")) |> ignore
  func.Statements.Add(VarNewDec "KeyAttributeCollection" "keys") |> ignore

  altKey.keyAttributes
  |> Array.iter (fun (logicalName, schemaName, ty) -> 
    func.Parameters.Add(
      CodeParameterDeclarationExpression(getAltKeyVarType ty, schemaName)) |> ignore
    func.Statements.Add(
      CodeExprMethodInvoke (VarRef "keys") "Add" None 
        [| StringLiteral(logicalName); VarRef schemaName |]) |> ignore
  )

  func.Parameters.Add(CodeParameterDeclarationExpression(sprintf "params Expression<Func<%s,object>>[]" entityName, "attrs")) |> ignore
  func.Statements.Add(Return <| MethodInvoke "Retrieve_AltKey" None 
    [| VarRef "service"; VarRef "keys"; VarRef "attrs" |]) |> ignore
    
  func.Comments.AddRange(
    CommentSummary [sprintf "Retrieves the record using the alternate key called '%s'" altKey.displayName])
      
  func :> CodeTypeMember

(** Entity Id Attributes *)
let MakeEntityIdAttributes (attr: XrmAttribute) =
  let guidType () = CodeTypeReference(typeof<Guid>.Name)
    
  let baseId = Variable "Id" (guidType())
  baseId.Attributes <- MemberAttributes.Public ||| MemberAttributes.Override
  baseId.CustomAttributes.Add(EntityAttributeLogicalNameAttribute attr.logicalName) |> ignore

  baseId.HasGet <- true
  baseId.GetStatements.Add(Return(BaseProp "Id")) |> ignore

  baseId.HasSet <- true
  baseId.SetStatements.Add(
    MethodInvoke "SetId" None 
      [|StringLiteral attr.logicalName; VarRef "value"|]) |> ignore
    
  let idAttrs = [|baseId :> CodeTypeMember;|]
  if attr.logicalName = "id" 
  then 
    idAttrs 
  else
    let attrId = MakeEntityAttribute Set.empty attr |> snd :?> CodeMemberProperty
    attrId.SetStatements.Clear()
    attrId.SetStatements.Add(
      MethodInvoke "SetId" None
        [|StringLiteral attr.logicalName; VarRef "value"|]) |> ignore

    Array.append idAttrs [|attrId :> CodeTypeMember|]

(** Entity OptionSet *)
let MakeEntityOptionSet (optSet: XrmOptionSet) =
  let enum = CodeTypeDeclaration(optSet.displayName)
  enum.IsEnum <- true
  enum.Attributes <- MemberAttributes.Public
  enum.CustomAttributes.Add(
    CodeAttributeDeclaration(AttributeName typeof<DataContractAttribute>)) |> ignore

  optSet.options
  |> Array.iter (fun option ->
    let field = Field option.label option.value
    field.CustomAttributes.Add(
      CodeAttributeDeclaration(AttributeName typeof<EnumMemberAttribute>)) |> ignore
    enum.Members.Add(field) |> ignore)

  enum

(** Entity Queryables *)
let MakeEntityQueryable (entity: XrmEntity) =
  let entityType = CodeTypeReference entity.schemaName
  let baseType = CodeTypeReference(typedefof<IQueryable<_>>.Name, entityType)
  let var = Variable (sprintf "%sSet" entity.schemaName) baseType
  var.GetStatements.Add(
    Return(MethodInvoke "CreateQuery" (Some entityType) [||])) |> ignore

  var :> CodeTypeMember


(** Entity *)
let MakeEntity (entity: XrmEntity) =
  let name = CrmNameHelper.entityMap.TryFind entity.logicalName ?| entity.schemaName
  let cl = Class name
  let baseProperties = baseReservedProperties |> Set.add name

  let getEnumType (getter: XrmEntity -> XrmOptionSet option) =
    match getter entity with
    | Some optSet -> CodeTypeReference(optSet.displayName)
    | None -> CodeTypeReference("EmptyEnum")

  let stateType = getEnumType (fun x -> x.stateAttribute)
  let statusType = getEnumType (fun x -> x.statusAttribute)
    
  // Comment summary
  match entity.description with
  | Some desc -> cl.Comments.AddRange(CommentSummary desc)
  | None -> ()

  // Setup the entity class
  cl.IsClass <- true
  cl.IsPartial <- true
  cl.BaseTypes.Add(ExtendedEntity stateType statusType) |> ignore
  entity.interfaces ?|> List.iter (fun i -> cl.BaseTypes.Add(i)) |> ignore
  cl.CustomAttributes.Add(EntityCustomAttribute entity.logicalName) |> ignore
  cl.CustomAttributes.Add(DebuggerDisplayAttribute()) |> ignore
  cl.CustomAttributes.Add(
    CodeAttributeDeclaration(AttributeName typeof<DataContractAttribute>)) |> ignore

  // Add static members
  cl.Members.AddRange(EntityConstructors()) |> ignore
  cl.Members.Add(Constant "EntityLogicalName" entity.logicalName) |> ignore
  if entity.typecode.IsSome then cl.Members.Add(Constant "EntityTypeCode" entity.typecode.Value) |> ignore
  cl.Members.Add(DebuggerDisplayMember entity.primaryNameAttribute) |> ignore

  // Static retrieve methods
  cl.Members.Add(MakeEntityStaticRetrieve name) |> ignore

  entity.alt_keys
  |> Array.ofList
  |> Array.fold 
    (fun keys k ->
      let key = MakeEntityAltKeyRetrieve name k
      key :: keys) []
  |> Array.ofList
  |> cl.Members.AddRange


  // Find id attribute
  let idAttrs, remainingAttrs = 
    entity.attr_vars 
    |> List.partition (fun x -> x.logicalName = entity.primaryIdAttribute)


  // Id attributes
  idAttrs |> List.tryHead |> function
  | Some attr -> 
    if attr.varType = Default typeof<Guid> then
      cl.Members.AddRange(MakeEntityIdAttributes attr)
  | None -> ()

    
  // Add attributes, relationships and alternative keys
  let addMembers (props,members:CodeTypeMember list) = 
    members |> Array.ofList |> Array.sortBy (fun m -> m.Name) |> cl.Members.AddRange; props

  let usedProps = 
    baseProperties 
    |> expandProps MakeEntityAttribute remainingAttrs |> addMembers
    |> expandProps MakeEntityRelationship entity.rel_vars |> addMembers
    |> expandProps MakeEntityAltKey entity.alt_keys |> addMembers


  // Create the enums related to the entity
  let globalOptionSets, relatedOptionSets =
    entity.opt_sets |> List.partition (fun optionSet -> optionSet.isGlobal)

  let relatedOptionSetEnums =
    relatedOptionSets |> List.map MakeEntityOptionSet |> List.sortBy (fun o -> o.Name)

  let globalOptionSetEnums =
    globalOptionSets |> List.map MakeEntityOptionSet |> List.sortBy (fun o -> o.Name)

  cl, relatedOptionSetEnums, globalOptionSetEnums
  
(** Query Context *)
let MakeContext (entities: XrmEntity[]) contextName =
  let sets = entities |> Array.map MakeEntityQueryable

  let context = CodeTypeDeclaration(contextName)
  context.IsClass <- true
  context.IsPartial <- true
  context.BaseTypes.Add(CodeTypeReference "ExtendedOrganizationServiceContext") |> ignore

  let cons = CodeConstructor()
  cons.Attributes <- MemberAttributes.Public
  cons.Parameters.Add(
    CodeParameterDeclarationExpression("IOrganizationService", "service")) |> ignore
  cons.BaseConstructorArgs.Add(CodeVariableReferenceExpression("service")) |> ignore

  context.Members.Add(cons) |> ignore

  context.Members.AddRange(sets)

  context

(** Intersection interface *)
let MakeIntersectInterface (intersect: XrmIntersect) =
  let name, attributes = intersect
  let intersect = Interface name

  intersect.BaseTypes.Add("IEntity");
  attributes
  |> List.iter (fun a -> intersect.Members.Add(MakeInterfaceAttribute Set.empty a) |> ignore)
    
  intersect

(** Create Standard CodeUnit **)
let CreateStandardCodeUnit ns =
  let cu = CodeCompileUnit()

  let globalNs = CodeNamespace()
  globalNs.Imports.Add(CodeNamespaceImport("System"))
  globalNs.Imports.Add(CodeNamespaceImport("System.Linq"))
  globalNs.Imports.Add(CodeNamespaceImport("System.Linq.Expressions"))
  globalNs.Imports.Add(CodeNamespaceImport("System.Diagnostics"))
  globalNs.Imports.Add(CodeNamespaceImport("System.Collections.Generic"))
  globalNs.Imports.Add(CodeNamespaceImport("System.Runtime.Serialization"))
  globalNs.Imports.Add(CodeNamespaceImport("System.ComponentModel"))
  globalNs.Imports.Add(CodeNamespaceImport("System.ComponentModel.DataAnnotations"))
  globalNs.Imports.Add(CodeNamespaceImport("Microsoft.Xrm.Sdk"))
  globalNs.Imports.Add(CodeNamespaceImport("Microsoft.Xrm.Sdk.Client"))
  globalNs.Imports.Add(CodeNamespaceImport("DG.XrmContext"))
  cu.Namespaces.Add(globalNs) |> ignore

  let ns = CodeNamespace(ns)
  cu.Namespaces.Add(ns) |> ignore

  cu, ns

(** Full Codeunit *)
let MakeCodeUnit (entities: XrmEntity[]) (intersects: XrmIntersect[]) ns context =
  //Create Standard CodeUnit with imports and assembly attributes
  let cu, ns = CreateStandardCodeUnit ns

  cu.AssemblyCustomAttributes.Add(
    CodeAttributeDeclaration(
      AttributeName typeof<ProxyTypesAssemblyAttribute>)) |> ignore

  let codeDomEntities, rOptSets, globalOptSets = 
    entities
    |> Array.map MakeEntity
    |> Array.unzip3

  let optSets = Array.append rOptSets globalOptSets

  let intersects = 
    intersects
    |> Array.map MakeIntersectInterface


  // Add types
  ns.Types.AddRange(intersects)
  ns.Types.AddRange(codeDomEntities)
    
  // Add context if specified
  match context with
  | Some contextName -> 
    ns.Types.Add(MakeContext entities contextName) |> ignore
  | None -> ()

  // Add option sets
  optSets
  |> List.concat 
  |> List.distinctBy (fun x -> x.Name)
  |> Array.ofList
  |> ns.Types.AddRange

  cu

(** Single File Entity CodeUnit **)
let MakeEntityCodeUnit (entity: XrmEntity) ns =
  //Create Standard CodeUnit with imports and assembly attributes
  let cu, ns = CreateStandardCodeUnit ns

  let codeDomEntity, optSets, globalOptSets = 
    entity
    |> MakeEntity

  // Add class
  ns.Types.Add(codeDomEntity) |> ignore

  // Add option sets
  optSets
  |> Array.ofList
  |> ns.Types.AddRange

  (cu, codeDomEntity.Name), globalOptSets

(** Single File Intersect CodeUnit **)
let MakeIntersectCodeUnit (intersect :XrmIntersect) ns =
 //Create Standard CodeUnit with imports and assembly attributes
  let cu, ns = CreateStandardCodeUnit ns

  let intersect = MakeIntersectInterface intersect

  //Add interface
  ns.Types.Add(intersect) |> ignore

  (cu, intersect.Name)

(** Enums Code Unit **)
let MakeEnumsCodeUnit ns (enumCodeTypeDeclerations: CodeTypeDeclaration list) =
 //Create empty code unit and add the namespace
  let cu = CodeCompileUnit()

  let globalNs = CodeNamespace()
  globalNs.Imports.Add(CodeNamespaceImport("System.Runtime.Serialization"))
  globalNs.Imports.Add(CodeNamespaceImport("Microsoft.Xrm.Sdk.Client"))
  cu.Namespaces.Add(globalNs) |> ignore

  cu.AssemblyCustomAttributes.Add(
    CodeAttributeDeclaration(
      AttributeName typeof<ProxyTypesAssemblyAttribute>)) |> ignore

  let ns = CodeNamespace(ns)
  cu.Namespaces.Add(ns) |> ignore

  //Add the enum code type declerations to a single code unit
  enumCodeTypeDeclerations
  |> List.distinctBy (fun x -> x.Name) 
  |> Array.ofList
  |> ns.Types.AddRange

  cu