using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

// URI metadata model
public class Attribute
{
    [JsonProperty("traitType")]
    public string TraitType { get; set; }

    [JsonProperty("value")]
    public string Value { get; set; }

    [JsonProperty("displayType")]
    public string DisplayType { get; set; }
}

public class NFTMetadataModel
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("category")]
    public string Category { get; set; }

    [JsonProperty("image")]
    public string Image { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("externalUrl")]
    public string ExternalUrl { get; set; }

    [JsonProperty("attributes")]
    public Attribute[] Attributes { get; set; } = Array.Empty<Attribute>();

    [JsonProperty("video")]
    public string AnimationUrl { get; set; }
}

public class CustomMetadataResolver : DefaultContractResolver
{
    private Dictionary<string, string> PropertyMappings { get; set; }

    public CustomMetadataResolver()
    {
        this.PropertyMappings = new Dictionary<string, string>
        {
            {"traitType", "trait_type"},
            {"externalUrl", "external_url"},
            {"displayType", "display_type"},
            {"video", "animation_url"},
        };
    }

    protected override string ResolvePropertyName(string propertyName)
    {
        string resolvedName = null;
        var resolved = this.PropertyMappings.TryGetValue(propertyName, out resolvedName);
        return (resolved) ? resolvedName : base.ResolvePropertyName(propertyName);
    }
}
