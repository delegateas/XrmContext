namespace DG.XrmContext

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

module internal XrmCodeDom =


  (** Entity Attribute *)
  let MakeEntityAttribute usedProps (attribute:XrmAttribute) =
    let funcName, varType, returnType = 
      match attribute.varType with
      | PartyList ->
        "EntityCollection",
        CodeTypeReference "ActivityParty" |> Some,
        CodeTypeReference(typedefof<IEnumerable<_>>.Name, CodeTypeReference "ActivityParty")

      | OptionSet enumName -> 
        "OptionSetValue", 
        CodeTypeReference enumName |> Some,
        CodeTypeReference (sprintf "%s?" enumName)

      | Default ty when ty.Equals(typeof<Money>) ->
        "MoneyValue", 
        None,
        CodeTypeReference("decimal?")

      | Default ty -> 
        let varType = getAttributeTypeRef ty
        sprintf "AttributeValue", Some varType, varType

    
    let name = CrmNameHelper.attributeMap.TryFind attribute.logicalName |? attribute.schemaName
    let validName = getValidName usedProps name
    let updatedUsedProps = usedProps.Add validName
    let var = Variable validName returnType
    var.CustomAttributes.Add(EntityAttributeCustomAttribute attribute.logicalName) |> ignore
    

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
      | true, false -> FieldRef typeof<EntityRole>.Name "Referencing" :> CodeExpression


    if relationship.referencing then
      var.CustomAttributes.Add(EntityAttributeCustomAttribute relationship.attributeName) |> ignore

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
  


  (** Entity Id Attributes *)
  let MakeEntityIdAttributes (attr: XrmAttribute) =
    let guidType () = CodeTypeReference(typeof<Guid>.Name)
    
    let baseId = Variable "Id" (guidType())
    baseId.Attributes <- MemberAttributes.Public ||| MemberAttributes.Override
    baseId.CustomAttributes.Add(EntityAttributeCustomAttribute attr.logicalName) |> ignore

    baseId.HasGet <- true
    baseId.GetStatements.Add(Return(BaseProp "Id")) |> ignore

    baseId.HasSet <- true
    baseId.SetStatements.Add(
      MethodInvoke "SetId" None 
        [|StringLiteral attr.logicalName; VarRef "value"|]) |> ignore
    

    let attrId = MakeEntityAttribute Set.empty attr |> snd :?> CodeMemberProperty
    attrId.SetStatements.Clear()
    attrId.SetStatements.Add(
      MethodInvoke "SetId" None
        [|StringLiteral attr.logicalName; VarRef "value"|]) |> ignore

    [|baseId :> CodeTypeMember; attrId :> CodeTypeMember|]


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
    let name = CrmNameHelper.entityMap.TryFind entity.logicalName |? entity.schemaName
    let cl = Class name
    let baseProperties = baseReservedProperties |> Set.add name

    let getEnumType getter =
      match getter entity with
      | Some optSet -> CodeTypeReference(optSet.displayName)
      | None -> CodeTypeReference("EmptyEnum")

    let stateType = getEnumType (fun x -> x.stateAttribute)
    let statusType = getEnumType (fun x -> x.statusAttribute)

    // Setup the entity class
    cl.IsClass <- true
    cl.IsPartial <- true
    cl.BaseTypes.Add(ExtendedEntity stateType statusType) |> ignore
    cl.CustomAttributes.Add(EntityCustomAttribute entity.logicalName) |> ignore
    cl.CustomAttributes.Add(DebuggerDisplayAttribute()) |> ignore
    cl.CustomAttributes.Add(
      CodeAttributeDeclaration(AttributeName typeof<DataContractAttribute>)) |> ignore

    // Add static members
    cl.Members.AddRange(EntityConstructors()) |> ignore
    cl.Members.Add(Constant "EntityLogicalName" entity.logicalName) |> ignore
    cl.Members.Add(Constant "EntityTypeCode" entity.typecode) |> ignore
    cl.Members.Add(DebuggerDisplayMember entity.primaryNameAttribute) |> ignore

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
    

    let usedProps, attrMembers = 
      remainingAttrs
      |> Array.ofList
      |> Array.fold 
        (fun (usedProps, attrs) e ->
          let newProps, attr = MakeEntityAttribute usedProps e
          newProps, attr :: attrs) 
        (baseProperties, [])
    
    attrMembers
    |> Array.ofList
    |> cl.Members.AddRange

    // Create entity relationship properties
    entity.rel_vars
    |> Array.ofList
    |> Array.fold 
      (fun (usedProps, rels) r ->
        let newProps, rel = MakeEntityRelationship usedProps r
        newProps, rel :: rels) 
      (usedProps, [])
    |> snd 
    |> Array.ofList
    |> cl.Members.AddRange

    // Create the enums related to the entity
    let optionSetEnums =
      entity.opt_sets |> List.map MakeEntityOptionSet

    cl, optionSetEnums


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


  (** Full Codeunit *)
  let MakeCodeUnit (entities: XrmEntity[]) ns context =
    let cu = CodeCompileUnit()

    let globalNs = CodeNamespace()
    globalNs.Imports.Add(CodeNamespaceImport("System"))
    globalNs.Imports.Add(CodeNamespaceImport("System.Linq"))
    globalNs.Imports.Add(CodeNamespaceImport("System.Diagnostics"))
    globalNs.Imports.Add(CodeNamespaceImport("System.Collections.Generic"))
    globalNs.Imports.Add(CodeNamespaceImport("System.Runtime.Serialization"))
    globalNs.Imports.Add(CodeNamespaceImport("Microsoft.Xrm.Sdk"))
    globalNs.Imports.Add(CodeNamespaceImport("Microsoft.Xrm.Sdk.Client"))
    globalNs.Imports.Add(CodeNamespaceImport("DG.XrmContext"))
    cu.Namespaces.Add(globalNs) |> ignore

    cu.AssemblyCustomAttributes.Add(
      CodeAttributeDeclaration(
        AttributeName typeof<ProxyTypesAssemblyAttribute>)) |> ignore
    
    let ns = CodeNamespace(ns)
    cu.Namespaces.Add(ns) |> ignore

    let codeDomEntities, optSets = 
      entities
      |> Array.map MakeEntity
      |> Array.unzip

    optSets
    |> List.concat 
    |> List.distinctBy (fun x -> x.Name)
    |> Array.ofList
    |> ns.Types.AddRange

    ns.Types.AddRange(codeDomEntities)
    match context with
    | Some contextName -> 
      ns.Types.Add(MakeContext entities contextName) |> ignore
    | None -> ()

    cu