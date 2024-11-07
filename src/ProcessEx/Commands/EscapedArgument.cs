using System.Management.Automation;

namespace ProcessEx.Commands;

[Cmdlet(
    VerbsData.ConvertTo, "EscapedArgument"
)]
[OutputType(typeof(string))]
public class ConvertToEscapedArgument : PSCmdlet
{
    [Parameter(
        Mandatory = true,
        Position = 0,
        ValueFromPipeline = true,
        ValueFromPipelineByPropertyName = true
    )]
    [AllowEmptyString]
    [AllowNull]
    public string[]? InputObject;

    [Parameter]
    public ArgumentEscapingMode ArgumentEscaping { get; set; } = ArgumentEscapingMode.Standard;

    protected override void ProcessRecord()
    {
        if (InputObject == null || InputObject.Length == 0)
        {
            WriteObject(ArgumentHelper.EscapeArgument(null, ArgumentEscaping));
            return;
        }

        foreach (string argument in InputObject)
            WriteObject(ArgumentHelper.EscapeArgument(argument, ArgumentEscaping));
    }
}
