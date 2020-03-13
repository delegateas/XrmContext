module internal DG.XrmContext.CodeDomHelper

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

let baseReservedProperties =
  typeof<Entity>.GetProperties() |> Array.map (fun x -> x.Name) |> Set.ofArray
  |> Set.add "EntityLogicalName"
  |> Set.add "EntityTypeCode"

let rec _getValidName props baseName i =
  let name = (sprintf "%s_%d" baseName i)
  if Set.contains name props then
    _getValidName props baseName (i+1)
  else 
    name

let getValidName props name =
  if Set.contains name props then
    _getValidName props name 1
  else 
    name
    
let simpleTypeNames = 
  [ (typeof<bool>.Name, "bool") 
    (typeof<int>.Name, "int") 
    (typeof<int64>.Name, "long") 
    (typeof<decimal>.Name, "decimal") 
    (typeof<double>.Name, "double")
  ] |> Map.ofList


let Class name = CodeTypeDeclaration(name)
let Interface name = CodeTypeDeclaration(name) |>> fun c -> c.IsInterface <- true

let Variable name ty = 
  CodeMemberProperty() 
  |> fun m -> 
    m.Name <- name
    m.Type <- ty    
    m.Attributes <- MemberAttributes.Public ||| MemberAttributes.Final
    m

let VoidType () = typeof<Void> |> CodeTypeReference
let Function name ty = 
  CodeMemberMethod() 
  |> fun m -> 
    m.Name <- name
    m.ReturnType <- ty
    m.Attributes <- MemberAttributes.Public ||| MemberAttributes.Final
    m
  
let VarRef = CodeVariableReferenceExpression
let FieldRef (ty:string) name = CodeFieldReferenceExpression(CodeTypeReferenceExpression(ty), name)
let TypeRef (ty:string) = CodeTypeReference(ty)
let Return = CodeMethodReturnStatement
let BaseProp propName = CodePropertyReferenceExpression(CodeBaseReferenceExpression(), propName)
let VarNewDec (ty:string) name = 
  CodeVariableDeclarationStatement(ty, name, CodeObjectCreateExpression(ty))

let This () = CodeThisReferenceExpression()
let CodeExprMethodInvoke codeExpr func (ty:CodeTypeReference option) parameters = 
  match ty with
  | None    -> 
    CodeMethodInvokeExpression(CodeMethodReferenceExpression(codeExpr, func), parameters)
  | Some ty ->
    CodeMethodInvokeExpression(CodeMethodReferenceExpression(codeExpr, func, ty), parameters)

let MemberMethodInvoke = VarRef >> CodeExprMethodInvoke
let MethodInvoke = CodeExprMethodInvoke null

let StringLiteral str = CodePrimitiveExpression(str)
  
let Field name (value: 'T) =
  let field = CodeMemberField(typeof<'T>, name)
  field.InitExpression <- CodePrimitiveExpression(value)
  field

let Constant name (value:'T) = 
  let field = Field name value
  field.Attributes <- MemberAttributes.Public ||| MemberAttributes.Const;
  field


let CommentSummary (content:string list) =
  content
  |> List.map (sprintf "<para>%s</para>")
  |> fun desc -> "<summary>" :: desc @ ["</summary>"]
  |> List.map (fun str -> CodeCommentStatement(str, true))
  |> Array.ofList
  |> CodeCommentStatementCollection


let AttributeName (ty:Type) = 
  match ty.Name.EndsWith("Attribute") with
  | true -> ty.Name.Remove(ty.Name.Length - 9)
  | false -> ty.Name

let getBaseAttributeTypeRef addNullable (ty:Type) =
  match ty.Name with
  | "String" -> CodeTypeReference ty
  | _ ->
    match simpleTypeNames.TryFind ty.Name, ty.IsValueType && addNullable with
    | None, false      -> CodeTypeReference ty.Name
    | None, true       -> CodeTypeReference (ty.Name + "?")
    | Some name, true  -> CodeTypeReference (name + "?")
    | Some name, false -> CodeTypeReference ty

let getSafeAttributeTypeRef ty =
  getBaseAttributeTypeRef true ty

let getStrictAttributeTypeRef ty =
  getBaseAttributeTypeRef false ty

let getAltKeyVarType = function
  | Default ty -> getStrictAttributeTypeRef ty
  | OptionSet name -> CodeTypeReference name
  | x -> failwithf "Invalid type for alternate key: %A" x

let expandProps func list baseProps =
  list |> Array.ofList |> Array.fold 
    (fun (usedProps, attrs) e ->
      let newProps, attr = func usedProps e
      newProps, attr :: attrs) 
    (baseProps, [])

(** Various helping builder functions *)
let ExtendedEntity ty1 ty2 = CodeTypeReference("ExtendedEntity", ty1, ty2)

let EntityConstructors () =
  let con1 = CodeConstructor()
  con1.Attributes <- MemberAttributes.Public
  con1.BaseConstructorArgs.Add(CodeVariableReferenceExpression("EntityLogicalName")) |> ignore
    
  let con2 = CodeConstructor()
  con2.Attributes <- MemberAttributes.Public
  con2.Parameters.Add(CodeParameterDeclarationExpression("Guid", "Id")) |> ignore
  con2.BaseConstructorArgs.Add(CodeVariableReferenceExpression("EntityLogicalName")) |> ignore
  con2.BaseConstructorArgs.Add(CodeVariableReferenceExpression("Id")) |> ignore
    
  [|con1 :> CodeTypeMember; con2 :> CodeTypeMember|]



let EntityCustomAttribute logicalName = 
  CodeAttributeDeclaration(AttributeName typeof<EntityLogicalNameAttribute>, 
    CodeAttributeArgument(StringLiteral(logicalName)))

let EntityAttributeCustomAttribute logicalName = 
  CodeAttributeDeclaration(AttributeName typeof<AttributeLogicalNameAttribute>, 
    CodeAttributeArgument(StringLiteral(logicalName)))

let RelationshipCustomAttribute schemaName entityRole = 
  let attr = 
    CodeAttributeDeclaration(AttributeName typeof<RelationshipSchemaNameAttribute>, 
      CodeAttributeArgument(StringLiteral(schemaName)))
  match entityRole with
  | Some role -> 
    attr.Arguments.Add(CodeAttributeArgument(role)) |> ignore 
    attr
  | None -> attr
   

let DebuggerDisplayAttribute () = 
  CodeAttributeDeclaration(AttributeName typeof<DebuggerDisplayAttribute>, 
    CodeAttributeArgument(StringLiteral("{DebuggerDisplay,nq}")))

let DebuggerDisplayMember nameAttr =
  let prop = CodeMemberProperty()
  prop.Type <- CodeTypeReference typeof<string>
  prop.Name <- "DebuggerDisplay"
  prop.Attributes <- MemberAttributes.Private
  prop.HasGet <- true
  prop.HasSet <- false

  prop.GetStatements.Add(
    Return(MethodInvoke "GetDebuggerDisplay" None [|StringLiteral nameAttr|])) |> ignore
  prop