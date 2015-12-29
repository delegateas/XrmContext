XrmContext
======================


<div class="row">
  <div class="span1"></div>
  <div class="span6">
    <div class="well well-small" id="nuget">
      The XrmContext executable can be 
      <a href="https://nuget.org/packages/Delegate.XrmContext">
        installed from NuGet
      </a>:
      <pre>PM> Install-Package Delegate.XrmContext</pre>
    </div>
  </div>
  <div class="span1"></div>
</div>


What is it?
-----------

XrmContext generates early-bound .NET framework classes which represent the entity
model of the given Dynamics CRM instance.
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

Click [here for more details](functionality.html) on each of these points.

  [crmsvcutil]: https://msdn.microsoft.com/en-us/library/gg327844.aspx


Getting Started
---------------

First see how to [generate the context](tool-usage.html), and afterwards check
out what you can [use the generated code for](functionality.html).