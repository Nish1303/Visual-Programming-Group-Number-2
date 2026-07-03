Run this once from FocusTrack.Data (with FocusTrack.UI as startup project) to generate
the initial EF Core migration, then commit the generated files here:

    dotnet ef migrations add InitialCreate --startup-project ../FocusTrack.UI

Do not hand-edit generated migration files.
