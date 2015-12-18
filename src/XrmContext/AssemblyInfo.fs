namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("XrmContext")>]
[<assembly: AssemblyProductAttribute("XrmContext")>]
[<assembly: AssemblyDescriptionAttribute("Tool to generate early-bound .NET framework classes and enumerations for MS CRM Dynamics server-side coding.")>]
[<assembly: AssemblyCompanyAttribute("Delegate A/S")>]
[<assembly: AssemblyCopyrightAttribute("Copyright (c) Delegate A/S 2015")>]
[<assembly: AssemblyVersionAttribute("1.1.0")>]
[<assembly: AssemblyFileVersionAttribute("1.1.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "1.1.0"
