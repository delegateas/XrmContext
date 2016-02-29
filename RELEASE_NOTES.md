# Release Notes

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