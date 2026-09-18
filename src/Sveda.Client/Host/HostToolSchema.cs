namespace Sveda.Host;

public static class HostToolSchema
{
    public static Dictionary<string, object?> Object(IReadOnlyDictionary<string, Dictionary<string, object?>> properties)
    {
        var normalized = new Dictionary<string, object?>();
        var required = new List<string>();

        foreach (var (name, definition) in properties)
        {
            var copy = new Dictionary<string, object?>(definition);
            if (copy.Remove("required", out var requiredFlag) && requiredFlag is true)
            {
                required.Add(name);
            }

            normalized[name] = copy;
        }

        var schema = new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = normalized,
        };

        if (required.Count > 0)
        {
            schema["required"] = required;
        }

        return schema;
    }
}
