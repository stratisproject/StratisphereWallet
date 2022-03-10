using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// URI metadata model
public class Attribute
{
    [JsonProperty("trait_type")]
    public string TraitType { get; set; }

    [JsonProperty("value")]
    public string Value { get; set; }

    [JsonProperty("display_type")]
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

    [JsonProperty("external_url")]
    public string ExternalUrl { get; set; }

    [JsonProperty("attributes")]
    public Attribute[] Attributes { get; set; } = Array.Empty<Attribute>();

    [JsonProperty("animation_url")]
    public string AnimationUrl { get; set; }
}
