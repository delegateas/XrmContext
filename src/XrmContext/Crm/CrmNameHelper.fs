module internal DG.XrmContext.CrmNameHelper

open System

open Microsoft.Xrm.Sdk
open Microsoft.Xrm.Sdk.Metadata

let entityMap = 
  [ ("activitymimeattachment", "ActivityMimeAttachment")
    ("monthlyfiscalcalendar", "MonthlyFiscalCalendar")
    ("fixedmonthlyfiscalcalendar", "FixedMonthlyFiscalCalendar")
    ("quarterlyfiscalcalendar","QuarterlyFiscalCalendar")
    ("semiannualfiscalcalendar", "SemiAnnualFiscalCalendar")
    ("annualfiscalcalendar","AnnualFiscalCalendar")
  ] |> Map.ofList

let attributeMap =
  [ ("month1", "Month1")
    ("month1_base", "Month1_Base")
    ("month2", "Month2")
    ("month2_base", "Month2_Base")
    ("month3", "Month3")
    ("month3_base", "Month3_Base")
    ("month4", "Month4")
    ("month4_base", "Month4_Base")
    ("month5", "Month5")
    ("month5_base", "Month5_Base")
    ("month6", "Month6")
    ("month6_base", "Month6_Base")
    ("month7", "Month7")
    ("month7_base", "Month7_Base")
    ("month8", "Month8")
    ("month8_base", "Month8_Base")
    ("month9", "Month9")
    ("month9_base", "Month9_Base")
    ("month10", "Month10")
    ("month10_base", "Month10_Base")
    ("month11", "Month11")
    ("month11_base", "Month11_Base")
    ("month12", "Month12")
    ("month12_base", "Month12_Base")
    ("quarter1", "Quarter1")
    ("quarter1_base", "Quarter1_Base")
    ("quarter2", "Quarter2")
    ("quarter2_base", "Quarter2_Base")
    ("quarter3", "Quarter3")
    ("quarter3_base", "Quarter3_Base")
    ("quarter4", "Quarter4")
    ("quarter4_base", "Quarter4_Base")
    ("firsthalf", "FirstHalf")
    ("firsthalf_base", "FirstHalf_Base")
    ("secondhalf", "SecondHalf")
    ("secondhalf_base", "SecondHalf_Base")
    ("annual", "Annual")
    ("annual_base", "Annual_Base")
    ("requiredattendees", "RequiredAttendees")
    ("from", "From")
    ("to", "To")
    ("cc", "Cc")
    ("bcc", "Bcc")
  ] |> Map.ofList