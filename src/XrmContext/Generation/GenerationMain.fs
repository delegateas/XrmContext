module DG.XrmContext.GenerationMain

open System
open Utility

open CrmBaseHelper
open DataRetrieval
open FileGeneration
open Setup

/// Retrieve data from CRM and setup raw state
let retrieveRawState xrmAuth (rSettings: XcRetrievalSettings) =
  let mainProxy = connectToCrm xrmAuth

  let proxyGetter = proxyHelper xrmAuth
  let entities = 
    getFullEntityList rSettings.entities rSettings.solutions mainProxy
      
  // Retrieve data from CRM
  retrieveCrmData entities mainProxy proxyGetter


/// Main generator function
let generateFromRaw gSettings (rawState: RawState) =
  let out = gSettings.out ?| "."
  let sdkVersion =
    gSettings.sdkVersion ?| rawState.crmVersion
  
  let entities = rawState.metadata |> Array.sortBy(fun e -> e.LogicalName)
  // Generate the files
  let interpretedData = interpretCrmData gSettings out sdkVersion rawState
  if gSettings.oneFile then createSingleFileCodeDom interpretedData else createMultiFileCodeDom interpretedData

  createResourceFiles out sdkVersion