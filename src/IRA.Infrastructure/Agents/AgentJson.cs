using System.Text.Json;

namespace IRA.Infrastructure.Agents;

/// <summary>Helpers for coaxing structured JSON out of LLM responses.</summary>
internal static class AgentJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Extracts the first JSON object/array from a model response, tolerating
    /// Markdown code fences and surrounding prose.
    /// </summary>
    public static string ExtractJson(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return "{}";
        }

        var text = response.Trim();

        // Strip ```json ... ``` fences.
        if (text.StartsWith("```"))
        {
            var firstNewline = text.IndexOf('\n');
            if (firstNewline >= 0)
            {
                text = text[(firstNewline + 1)..];
            }

            var fenceEnd = text.LastIndexOf("```", StringComparison.Ordinal);
            if (fenceEnd >= 0)
            {
                text = text[..fenceEnd];
            }
        }

        text = text.Trim();
        var objStart = text.IndexOf('{');
        var arrStart = text.IndexOf('[');
        var start = (objStart, arrStart) switch
        {
            ( < 0, < 0) => -1,
            ( < 0, _) => arrStart,
            (_, < 0) => objStart,
            _ => Math.Min(objStart, arrStart)
        };

        if (start < 0)
        {
            return "{}";
        }

        var lastObj = text.LastIndexOf('}');
        var lastArr = text.LastIndexOf(']');
        var end = Math.Max(lastObj, lastArr);
        return end > start ? text[start..(end + 1)] : text[start..];
    }

    public static T? TryDeserialize<T>(string response)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(ExtractJson(response), Options);
        }
        catch (JsonException)
        {
            return default;
        }
    }
}
