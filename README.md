# XrmContext ![Appveyor build status](https://ci.appveyor.com/api/projects/status/github/delegateAS/XrmContext?svg=true&branch=master) [![NuGet version](https://badge.fury.io/nu/Delegate.XrmContext.svg)](https://badge.fury.io/nu/Delegate.XrmContext) [![Join the chat at https://gitter.im/delegateas/XrmContext](https://badges.gitter.im/delegateas/XrmContext.svg)](https://gitter.im/delegateas/XrmContext?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

XrmContext generates early-bound .NET classes which represent the entity
model of the given Dynamics CRM instance.

XrmDefinitelyTyped can be [installed and updated via NuGet](https://nuget.org/packages/Delegate.XrmContext)

```PM> Install-Package Delegate.XrmContext```

What is it?
-----------

It is very similar to that of [CrmSvcUtil][crmsvcutil], but has several new features that 
help you code better and more reliably, and generates files that takes up less space.

Features include:

* Much smaller code files
* Both entity- and solution-based filtering available
* Option sets are generated as enums
* Attributes of type `OptionSetValue` (and others) have been abstracted away, 
  and strongly-typed, to allow for cleaner and more robust code.
* Additional helper methods for both entities and the service context have been added.
* Simple deprecation of attributes/fields
* DebuggerDisplay attributes on entities

Click [here for more details](https://github.com/delegateas/XrmContext/wiki/Functionality) on each of these points.

  [crmsvcutil]: https://msdn.microsoft.com/en-us/library/gg327844.aspx


Getting Started
---------------

First see how to [generate the context](https://github.com/delegateas/XrmContext/wiki/Tool-usage), and afterwards check
out what you can [use the generated code for](https://github.com/delegateas/XrmContext/wiki/Functionality).
