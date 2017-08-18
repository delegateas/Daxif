(**
View Extender
=============
*)

#load @"_Config.fsx"
open _Config
open DG.Daxif

View.Generate(Env.dev, Path.daxifRoot,
  solutions = [|
    SolutionInfo.name
    |],
  entities = [|
    // eg. "systemuser"
    |])


#load @"viewExtenderData\_ViewGuids.fsx" 
#load @"viewExtenderData\_EntityRelationships.fsx" 
#load @"viewExtenderData\_EntityAttributes.fsx"

open ViewGuids
open EntityAttributes
open EntityRelationships

// define extensions e.g.:
// Views.Appointment.MyAppointments
// |> View.Parse Env.dev
// |> View.Extend Views.Appointment.MyAppointments1
// |> View.AddColumnFirst Appointment.Fields.Actualstart 123
// |> View.AddColumnFirst Appointment.Fields.Actualend 37
// |> View.UpdateView Env.dev