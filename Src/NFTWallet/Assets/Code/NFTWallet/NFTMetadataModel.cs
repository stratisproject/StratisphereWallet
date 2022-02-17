using System.Collections.Generic;

// URI metadata model
public class Attribute
{
    public string traitType { get; set; }
    public string value { get; set; }
}

public class NFTMetadataModel
{
    public string description { get; set; }
    public string externalUrl { get; set; }
    public string image { get; set; }
    public string name { get; set; }
    public List<Attribute> attributes { get; set; }
    public string category { get; set; }
}
