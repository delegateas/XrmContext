# Release Notes
### 3.0.0 - 10 June 2022
* Added XrmExtensions to generated namespace

### 2.0.2 - 08 July 2021
* Added support for multiple locale optionset labels (@sjkp)

### 2.0.1 - 21 May 2021
* Added shortcut for primary name field so you don't have to know it
* Added relationship schema name to intersect entities

### 2.0.0 - 05 January 2020
* Added new dependency to System.Componentmodel.DataAnnotations

### 1.8.1 - December 02 2020
* Fixed error when multioptionset had no options

### 1.8.0 - September 07 2020
* Added option for ignore Object Type Codes
* Ordered output from option sets

### 1.7.6 - August 10 2020
* Fix an issue in `XrmExtensions.cs` when working with multi select option set (@pksorensen)

### 1.7.5 - June 17 2020
* Added Connection String Authentication method

### 1.7.4 - March 13 2020
* Now ignores private entities and files are ordered by schema name (@rajyraman)

### 1.7.3 - March 13 2020

### 1.7.2 - February 25 2020
* Fixed error when an alternate key used an optionset attribute

### 1.7.1 - February 13 2020
* Fix assembly reference issue when calling from Daxif

### 1.7.0 - February 11 2020
* Added support for client secret authentication

### 1.6.0 - September 24 2019
* Added support for MFA

### 1.5.4 - May 24 2019
* Added support for emojis through label mappings 

### 1.5.3 - February 18 2019
* Added support for multi-option set

### 1.5.2 - August 17 2018
* Reduced the amount of proxies connected to the environment

### 1.5.1 - July 19 2018
* Updated CRM assemblies to latest version (from v8 to v9)
* Updated other dependencies to latest version as well
* Included FSharp.Core to be able to build nuget package
* Removed Microsoft.IdentityModel.dll under the assumption that it is no longer needed (for comparison XDT does not include it)

### 1.5.0 - January 5 2018
* Updated .Net Framework from 4.5.2 to 4.6.2
* Inner exceptions are printed (rather than just outmost exception message)
* Ensured that XrmContext fetches the correct attribute logical name in instance where attribute has the same name as the entity

### 1.4.5 - October 11 2017
* Attributes of attributes, like Account.AccountParentIdName are no longer generated
* Attributes, Relationships and Alt Keys for entities are now ordered alphabetically in their classes

### 1.4.4 - September 20 2017
* Setting the new "***/oneFile***" argument to false will now generate one file per entity instead of one big file. (Remember to include all the files in your project if your are switching over.)

### 1.4.3 - September 15 2017
* Entities with PrimaryEntityId of id no longer causes an error in the generated file

### 1.4.2 - September 12 2017
* Fixed building without specified solution(s)

### 1.4.1 - May 24 2017
* Fixed certain arguments not being parsed correctly

### 1.4.0 - May 22 2017
* Removed NuGet dependencies and added necessary assemblies directly as files -- making it easy to use the tool straight from NuGet

### 1.3.1 - April 4 2017
* Fixed automatic CRM version check

### 1.3.0 - February 10 2017
* Added the possibility to intersect entities, in order to generate interfaces that only contain common attributes
* Added functionality to easily determine changes done to attributes of a record with `TagForDelta` and `PerformDelta`

### 1.2.4 - September 21 2016
* Fixed CustomAttribute on relationships that were of type `EntityRole.Referenced`

### 1.2.3 - July 7 2016
* Added new extension helper methods to Entity: `ContainsAttributes`, `RemoveAttributes` 

### 1.2.2 - April 15 2016
* Added support for entities and global option sets sharing the same name

### 1.2.1 - April 7 2016
* Fixed PerformAsBulk sending too many requests
* Ensures distinct entity logical names
* Argument names are now case-insensitive

### 1.2.0 - March 29 2016
* Added functionality that helps with alternate keys for entities, 
  and in order to support this, the dependency `Microsoft.CrmSdk.CoreAssemblies` has been increased to the newest version again
* Added a comment description for entity classes
* Added several functions to easily create CRM requests from entity objects
* Added static retrieve methods (`.Retrieve_<keyname>`) on entity classes that makes it possible to retrieve records using alternate keys
* Added methods (`.AltKey_<keyname>`) that helps set the alternate keys correctly on an entity object before upserting it
* Added `.Retrieve<T>`, `.Upsert`, `.Assign` and `.SetState` extension methods on `IOrganizationService` to simplify the use of these requests
* Added `.PerformAsBulk` extension method on `IOrganizationService` that performs requests as bulk
* To support multiple versions of `CrmSdk.CoreAssemblies` in the target library, a new "***/sdkversion***" argument has been added

### 1.1.2 - February 24 2016
* Added "***/genconfig***" argument which generates a dummy configuration file to use
* Added "***/useconfig***" argument, see [usage for more information](tool-usage.html#Configuration-file)
* Added version print when using the executable
* Changed exit-code for the executable to be 1 instead of 0, when it encounters an exception

### 1.1.1 - December 29 2015
* Improved retrieval of CRM metadata
* Fixed incorrect argument description

### 1.1.0 - December 18 2015
* Reduced version requirements to support backward-compatibility:
  * Reduced requirement of the dependency `Microsoft.CrmSdk.CoreAssemblies` to 5.0.18 or greater
  * Reduced used .NET Framework to version 4.5.2

### 1.0.1 - December 07 2015
* Fixed an incorrect string set in some `AttributeLogicalName` attributes.

### 1.0.0 - October 15 2015
* Initial public release