using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using static System.Console;

static class Program
{
    const string InputExtension = ".rdfe";
    const string OutputExtension = ".rdfx";

    // exit codes
    const int StatusSuccess = 0;
    const int StatusUsageError = 1;
    const int StatusConversionError = 2;
    
    static int Main(string[] args)
    {
        if (args.Length != 1) 
            return UsageError($"Required <file{InputExtension}> or <directory> path not specified");

        var path = args[0];
        if (!Path.Exists(path)) 
            return UsageError($"Specified path '{path}' not found");
        
        if (File.Exists(path) && Path.GetExtension(path) is not InputExtension)
            return UsageError($"Input file '{path}' must have {InputExtension} extension");

        try
        {
            if (File.Exists(path)) ConvertJsonFile(path);
            else ConvertDirectoryFiles(path);
            
            return StatusSuccess;
        }
        catch (Exception ex)
        {
            Error.WriteLine($"Error: {ex}");
            return StatusConversionError;
        }
    }

    static void ConvertJsonFile(string path)
    {
        WriteLine($"Reading file '{path}'");
        
        using FileStream stream = File.OpenRead(path);
        var data = JsonSerializer.Deserialize<DynamicFolderExport>(stream, new JsonSerializerOptions
        {
            IncludeFields = false,
            MaxDepth = 8,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        });
        Debug.Assert(data is not null);

        string outputPath = Path.ChangeExtension(path, OutputExtension);

        WriteLine($"Writing data to {outputPath}");
        ConvertDataToXml(data, outputPath);
    }

    static void ConvertDataToXml(DynamicFolderExport export, string path)
    {
        using FileStream stream = File.OpenWrite(path);
        using var xml = XmlWriter.Create(stream, new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = true,
            IndentChars = "    ",
            CloseOutput = true,
            ConformanceLevel = ConformanceLevel.Fragment,
            NewLineHandling = NewLineHandling.None,
            OmitXmlDeclaration = true,
            WriteEndDocumentOnClose = true,
        });
        
        xml.WriteStartElement(nameof(DynamicFolderExport));
        WriteStringValue(xml, nameof(export.Name), export.Name);
        
        xml.WriteStartElement(nameof(export.Objects));
        foreach (var @object in export.Objects)
        {
            xml.WriteStartElement(nameof(DynamicFolderExportObject));
            WriteStringValue(xml, nameof(@object.Type), @object.Type);
            WriteStringValue(xml, nameof(@object.Name), @object.Name);
            WriteStringValue(xml, nameof(@object.Description), @object.Description);
            WriteStringValue(xml, nameof(@object.Notes), @object.Notes);

            xml.WriteStartElement(nameof(@object.CustomProperties));
            foreach (var prop in @object.CustomProperties)
            {
                xml.WriteStartElement(nameof(CustomProperty));
                WriteStringValue(xml, nameof(prop.Name), prop.Name);
                WriteStringValue(xml, nameof(prop.Type), prop.Type);
                WriteStringValue(xml, nameof(prop.Value), prop.Value);
                xml.WriteEndElement(/* nameof(CustomProperty) */);
            }
            xml.WriteEndElement(/* nameof(@object.CustomProperties) */);
            
            WriteStringValue(xml, nameof(@object.ScriptInterpreter), @object.ScriptInterpreter);
            WriteStringValue(xml, nameof(@object.Script), @object.Script);

            WriteStringValue(xml, nameof(@object.DynamicCredentialScriptInterpreter), @object.DynamicCredentialScriptInterpreter);
            WriteStringValue(xml, nameof(@object.DynamicCredentialScript), @object.DynamicCredentialScript);
            
            xml.WriteEndElement(/* nameof(DynamicFolderExportObject) */);
        }
        
        xml.WriteEndElement(/* nameof(data.Objects) */);
        xml.WriteEndElement(/* nameof(DynamicFolderExport) */);
        
        xml.Flush();
        stream.Flush();
    }

    static void WriteStringValue(XmlWriter writer, string elementName, string? value)
    {
        if (string.IsNullOrEmpty(value) || !value.Contains('\n'))
            writer.WriteElementString(elementName, value);
        else {
            writer.WriteStartElement(elementName);
            writer.WriteCData(value);
            writer.WriteEndElement();
        }
    }

    static void ConvertDirectoryFiles(string path)
    {
        foreach (var file in Directory.EnumerateFiles(path, "*" + InputExtension, SearchOption.AllDirectories))
            ConvertJsonFile(file);
    }
    
    static int UsageError(string message)
    {
        string programName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly()!.Location);
       
        Error.WriteLine(message);
        Error.WriteLine();
        Error.WriteLine($"Usage: {programName} <file{InputExtension} | directory>");
        Error.WriteLine($"   file{InputExtension}: write the equivalent {OutputExtension} side-by-side");
        Error.WriteLine($"   directory: convert all {InputExtension} files and write {OutputExtension} side-by-side");
        return StatusUsageError;
    }
}

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable CollectionNeverUpdated.Global
sealed class DynamicFolderExport
{
    public string? Name { get; set; }
    public List<DynamicFolderExportObject> Objects { get; set; } = new();
}

sealed class DynamicFolderExportObject
{
    public string? Type { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public List<CustomProperty> CustomProperties { get; set; } = new();
    public string? Script { get; set; }
    public string? ScriptInterpreter { get; set; }
    public string? DynamicCredentialScriptInterpreter { get; set; }
    public string? DynamicCredentialScript { get; set; }
}

sealed class CustomProperty
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Value { get; set; }
}