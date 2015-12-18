Functionality compared to CrmSvcUtil
=============

List of functionality that differentiates XrmContext from CrmSvcUtil.


Generated code takes up less disk space
---------------------------------------
**CrmSvcUtil** generates all types with their full namespace declaration, and has a lot of repeated code
chunks. This usually leads to the generated code files being quite large, and this can cause issues
when trying to upload it to a CRM system that has a limit on how big assemblies are allowed.

With **XrmContext** the full namespace declarations for types has been removed by just having them imported 
at the top of the generated code instead.
Besides this, all of the repeated code chunks have been moved into a new parent class named `ExtendedEntity`, such
that the generated entity code takes up much less space.



Entity- and solution-based filtering 
------------------------------------
For CrmSvcUtil it was possible to write an extension that could [filter the generated code][crmsvcutil-filter].

In XrmContext, this feature comes out-of-the-box for entities. When the code is run, you can specify which
entities it should include with the `entities` argument (see [Usage](tool-usage.html)).

To further help, you also have the option to automatically include all entities contained in given solutions 
with the `solutions` argument.



Option sets as enums
--------------------
All necessary option sets are now generated as enumerations. For CrmSvcUtil this could also be achieved,
but it had done with [an extension to the program][crmsvcutil-enum].



Difference in attribute types
-----------------------------
To ensure coding correctness, as well as making it more straight-forward to make sensible code, 
some abstractions have been made to the types of certain attributes in CRM.

  * **OptionSetValue**: is now the corresponding **enum** for the option set (nullable)
  * **Money**: is now the money value itself (`decimal`)
  * **EntityCollection**: is now simplified to an `IEnumerable<Entity>`

Especially the abstraction made with option sets by XrmContext makes for more robust and readable code.
Below is a comparison of how option set values are handled in plugin/workflow code with different 
levels of abstraction.

First off is the standard way to do it with out-of-the-box **CrmSvcUtil**, which just uses 
the `OptionSetValue` class with integers (and hopefully comments) to indicate which 
option set value is desired:

<img src="img/osv-c1.png" class="code" />

With the added enumerations from the **CrmSvcUtil** extension, it becomes a bit more sensible code:

<img src="img/osv-c2.png" class="code" />

The problem here is that the `OptionSetValue` encapsulates the actual integer value, 
which means we have no way of knowing what values that attribute is actually allowed to take.

This is where **XrmContext** performs abstractions behind-the-scenes, such that
we can arrive at a much more optimal way of writing it:

<img src="img/osv-c3.png" class="code" />

When it is typed like this, you ***can only*** write correct option set values, and thus the code is way more 
robust and easier to read. And you even get intellisense for the necessary enum when you arrive at that 
part of the code.

> Note: Of course you *can* bypass the enum restriction by doing some clever casting, 
> but if you get to that point, you may want to reconsider what you are trying to code.



Additional helper functions
---------------------------

Additional functions have been made that makes it easier to code correctly and
adds shortcut-methods for often-used functionality:

  * Load a property directly: `<context>.Load(..)`
  * Load an enumeration (i.e. related entities): `<context>.LoadEnumeration(..)`
  * Set state of entity: `<context>.SetState(..)` *or* `<entity>.SetState(..)`



Deprecation prefix
------------------

Simple deprecation of attributes/fields by giving them a prefix to their Display Name in CRM. 
This prefix is specified with the `deprecatedprefix` argument to this program.

Attributes found that have the given prefix will be marked with an [Obsolete flag][obsolete-flag],
which makes warnings appear in your build when such an attribute is actively used in your code.


DebuggerDisplay attribute
-------------------------

Added [DebuggerDisplay][debuggerdisplay] attributes to each entity that shows a more detailed
description of the object, instead of just "Entity".


  [crmsvcutil-enum]: https://msdn.microsoft.com/en-us/library/00533626-2587-4bb2-ad82-98560024794e#Generate_Enums
  [crmsvcutil-filter]: http://erikpool.blogspot.dk/2011/03/filtering-generated-entities-with.html
  [obsolete-flag]: https://msdn.microsoft.com/en-us/library/22kk2b44(v=vs.90).aspx
  [debuggerdisplay]: https://msdn.microsoft.com/en-us/library/x810d419.aspx
