using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using System.Globalization;

namespace ProcessEx;

public sealed class EncodingOrByteStream
{
    public Encoding? Encoding { get; }

    public bool IsByteStream { get; }

    public EncodingOrByteStream()
    {
        Encoding = null;
        IsByteStream = true;
    }

    public EncodingOrByteStream(Encoding encoding)
    {
        Encoding = encoding;
        IsByteStream = false;
    }
}

public sealed class EncodingOrByteStreamTransformAttribute : ArgumentTransformationAttribute
{
    internal static string[] KnownEncodings = [
        "UTF8",
        "ConsoleInput",
        "ConsoleOutput",
        "ASCII",
        "ANSI",
        "Bytes",
        "OEM",
        "Unicode",
        "UTF8Bom",
        "UTF8NoBom"
    ];

    public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
    {
        if (inputData is PSObject psObj)
        {
            inputData = psObj.BaseObject;
        }

        if (inputData is string inputString && inputString.ToUpperInvariant() == "BYTES")
        {
            return new EncodingOrByteStream();
        }

        Encoding encoding = inputData switch
        {
            Encoding e => e,
            string s => GetEncodingFromString(s.ToUpperInvariant()),
            int i => Encoding.GetEncoding(i),
            _ => throw new ArgumentTransformationMetadataException($"Could not convert input '{inputData}' to a valid Encoding object."),
        };
        return new EncodingOrByteStream(encoding);
    }

    private static Encoding GetEncodingFromString(string encoding) => encoding switch
    {
        "ASCII" => new ASCIIEncoding(),
        "ANSI" => Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.ANSICodePage),
        "BIGENDIANUNICODE" => new UnicodeEncoding(true, true),
        "BIGENDIANUTF32" => new UTF32Encoding(true, true),
        "CONSOLEINPUT" => Console.InputEncoding,
        "CONSOLEOUTPUT" => Console.OutputEncoding,
        "OEM" => Console.OutputEncoding,
        "UNICODE" => new UnicodeEncoding(),
        "UTF8" => new UTF8Encoding(),
        "UTF8BOM" => new UTF8Encoding(true),
        "UTF8NOBOM" => new UTF8Encoding(),
        "UTF32" => new UTF32Encoding(),
        _ => Encoding.GetEncoding(encoding),
    };
}

#if NET6_0_OR_GREATER
public class EncodingOrByteStreamCompletionsAttribute : ArgumentCompletionsAttribute
{
    public EncodingOrByteStreamCompletionsAttribute() : base(EncodingOrByteStreamTransformAttribute.KnownEncodings)
    { }
}
#else
public class EncodingOrByteStreamCompletionsAttribute : IArgumentCompleter {
    public IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters
    )
    {
        if (string.IsNullOrWhiteSpace(wordToComplete))
        {
            wordToComplete = "";
        }

        WildcardPattern pattern = new($"{wordToComplete}*");
        foreach (string encoding in EncodingOrByteStreamTransformAttribute.KnownEncodings)
        {
            if (pattern.IsMatch(encoding))
            {
                yield return new CompletionResult(encoding);
            }
        }
    }
}
#endif
