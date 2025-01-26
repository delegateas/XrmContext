namespace DG.XrmContext

open System
open Microsoft.Xrm.Sdk.Client
open Microsoft.Xrm.Sdk.Metadata
open System.Runtime.Serialization
open Microsoft.IdentityModel.Clients.ActiveDirectory

type Version = int * int * int * int
type EntityIntersect = string * string[]

type ConnectionType = 
  | Proxy
  | OAuth
  | ClientSecret
  | ConnectionString   

type XrmAuthentication = {
  url: Uri
  method: ConnectionType option
  username: string option
  password: string option
  domain: string option
  ap: AuthenticationProviderType option
  clientId: string option
  returnUrl: string option
  clientSecret: string option
  connectionString: string option
  prompt: PromptBehavior option
}

type XcGenerationSettings = {
  out: string option
  ns: string option
  context: string option
  deprecatedPrefix: string option
  sdkVersion: Version option
  intersections: EntityIntersect[] option
  labelMapping: (string * string)[] option
  localizations: int[] option
  oneFile: bool
  includeEntityTypeCode: bool
}

type XcRetrievalSettings = {
  entities: string[] option
  solutions: string[] option
}

type SolutionFileSettings = {
  solutionFolderPath: string
}

type LocalizedLabel = {
  Label: string
  LanguageCode: int
}

type LabelGroup = {
  UserLocalizedLabel: LocalizedLabel
  LocalizedLabels: LocalizedLabel array
}


type RawAttributeFields = {
  SchemaName: string
  LogicalName: string
  DisplayName: LabelGroup
  Description: LabelGroup
  AttributeType: AttributeTypeCode
  IsValidForCreate: bool
  IsValidForRead: bool
  IsValidForUpdate: bool
  AttributeOf: string
}

type EnumOption = {
  Label: LabelGroup
  Description: LabelGroup
  Color: string
  Value: int
}

type EnumAttributeFields = {
  Options: EnumOption[]
  OptionSetType: OptionSetType
  IsGlobal: bool
  SchemaName: string
  Name: string
  DisplayName: LabelGroup
}
type EnumAttribute = RawAttributeFields * EnumAttributeFields

type StringAttributeFields = {
  MaxLength: int option
}

type IntAttributeFields = {
  MinValue: int option
  MaxValue: int option
}

type EnumCollectionAttribute = RawAttributeFields

type VirtualAttribute = 
  | OptionSetCollectionAttribute of EnumCollectionAttribute

type RawAttribute = 
  | StringAttribute of RawAttributeFields * StringAttributeFields
  | IntAttribute of RawAttributeFields * IntAttributeFields
  | BigIntAttribute of RawAttributeFields
  | BooleanAttribute of RawAttributeFields
  | DateTimeAttribute of RawAttributeFields
  | DecimalAttribute of RawAttributeFields
  | DoubleAttribute of RawAttributeFields
  | EnumAttribute of EnumAttribute
  | FileAttribute of RawAttributeFields
  | ImageAttribute of RawAttributeFields
  | LookupAttribute of RawAttributeFields
  | MemoAttribute of RawAttributeFields
  | MoneyAttribute of RawAttributeFields
  | VirtualAttribute of VirtualAttribute

type RawAttributeType =
  | BigInt
  | Boolean
  | DateTime
  | Decimal
  | Double
  | Enum
  | File
  | Image
  | Int
  | Lookup
  | Memo
  | Money
  | String


type RawKey = {
  DisplayName: LabelGroup
  SchemaName: string
  Attributes: string array
}

type RawOneToMany = {
  SchemaName: string
  ReferencingEntity: string
  ReferencingAttribute: string
  ReferencedEntity: string
  ReferencedAttribute: string
}

type RawManyToMany = {
  SchemaName: string
  Entity1LogicalName: string
  Entity1IntersectAttribute: string
  Entity2LogicalName: string
  Entity2IntersectAttribute: string
}

type RawEntity = {
  LogicalName: string
  SchemaName: string
  ObjectTypeCode: int option
  DisplayName: LabelGroup
  Description: LabelGroup
  IsPrivate: bool
  Attributes: RawAttribute array
  Keys: RawKey array
  OneToManyRelationships: RawOneToMany array
  ManyToOneRelationships: RawOneToMany array
  ManyToManyRelationships: RawManyToMany array
  IsIntersect: bool
  PrimaryNameAttribute: string
  PrimaryIdAttribute: string
}

/// Serializable record containing necessary (meta)data
[<DataContract>]
type RawState = {

  [<field : DataMember>]
  crmVersion: Version

  [<field : DataMember>]
  metadata: RawEntity[]
}
