using AvionRelay.Core.Messages.MessageTypes;

namespace AvionRelay.Examples.SharedLibrary.Inspections;




public class GetLanguageInspection : Inspection<LanguageInspectionResponse>
{
    public GetLanguageInspection()
    {
        Metadata.MessageTypeName = nameof(GetLanguageInspection);
    }
    
}


public class LanguageInspectionResponse
{
    public required string Language { get; set; }
}