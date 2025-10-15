using MYA.RequestProtect.Options;
using Newtonsoft.Json;
using NJsonSchema;
using NJsonSchema.Generation;
using System.Text.Json;
using System.Text.Json.Nodes;

try
{
    var outputPath = GetOutputPath(args);
    var schema = GenerateBaseSchema();
    var wrappedSchema = WrapSchemaWithConfigurationKey(schema, RequestProtectOptions.Key);

    WriteSchemaToFile(wrappedSchema, outputPath);

    Console.WriteLine($"✓ Schema generated successfully at: {outputPath}");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"✗ Error generating schema: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
    Environment.Exit(1);
}

static string GetOutputPath(string[] args) => args.Length > 0 ? args[0] : "appsettings-schema.MYA.RequestProtect.json";

static JsonSchema GenerateBaseSchema()
{
    var settings = new SystemTextJsonSchemaGeneratorSettings
    {
        DefaultReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull,
        SchemaType = SchemaType.JsonSchema
    };

    return JsonSchema.FromType<RequestProtectOptions>(settings);
}

static JsonObject WrapSchemaWithConfigurationKey(JsonSchema schema, string configurationKey)
{
    var schemaJson = schema.ToJson(Formatting.None);
    var schemaNode = JsonNode.Parse(schemaJson);

    var keyParts = configurationKey.Split(':');

    var wrappedNode = CreateRootSchemaNode(schemaNode);
    var propertiesNode = CreatePropertiesNode(wrappedNode);

    BuildNestedStructure(propertiesNode, keyParts, schemaNode);

    return wrappedNode;
}

static JsonObject CreateRootSchemaNode(JsonNode? schemaNode)
{
    var root = new JsonObject
    {
        ["type"] = "object"
    };

    // Copy definitions if they exist
    if (schemaNode?["definitions"] != null)
    {
        root["definitions"] = schemaNode["definitions"]!.DeepClone();
    }

    return root;
}

static JsonObject CreatePropertiesNode(JsonObject rootNode)
{
    var propertiesNode = new JsonObject();
    rootNode["properties"] = propertiesNode;
    return propertiesNode;
}

static void BuildNestedStructure(JsonObject currentLevel, string[] keyParts, JsonNode? schemaNode)
{
    for (int i = 0; i < keyParts.Length; i++)
    {
        var keyPart = keyParts[i];
        var isLast = i == keyParts.Length - 1;

        if (isLast)
        {
            currentLevel[keyPart] = CreateFinalSchemaNode(schemaNode);
        }
        else
        {
            var nestedNode = CreateNestedObjectNode();
            currentLevel[keyPart] = nestedNode;
            if (nestedNode["properties"] is not null)
                currentLevel = nestedNode["properties"]!.AsObject();
        }
    }
}

static JsonObject CreateFinalSchemaNode(JsonNode? schemaNode)
{
    var finalNode = new JsonObject
    {
        ["type"] = "object"
    };

    // Copy properties from the original schema
    if (schemaNode?["properties"] != null)
    {
        finalNode["properties"] = schemaNode["properties"]!.DeepClone();
    }

    // Copy other relevant fields
    if (schemaNode?["additionalProperties"] != null)
    {
        finalNode["additionalProperties"] = schemaNode["additionalProperties"]!.DeepClone();
    }

    if (schemaNode?["description"] != null)
    {
        finalNode["description"] = schemaNode["description"]!.DeepClone();
    }

    if (schemaNode?["title"] != null)
    {
        finalNode["title"] = schemaNode["title"]!.DeepClone();
    }

    return finalNode;
}

static JsonObject CreateNestedObjectNode()
{
    return new JsonObject
    {
        ["type"] = "object",
        ["properties"] = new JsonObject()
    };
}

static void WriteSchemaToFile(JsonObject schemaNode, string outputPath)
{
    var jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    var json = schemaNode.ToJsonString(jsonOptions);
    File.WriteAllText(outputPath, json);
}