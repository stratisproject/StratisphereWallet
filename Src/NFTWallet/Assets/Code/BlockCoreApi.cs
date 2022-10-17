using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class BlockCoreApi
{
    private readonly HttpClient client;

    private readonly string baseUri;

    public BlockCoreApi(string baseUri)
    {
        this.baseUri = baseUri;
        client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<List<OwnedNFTItem>> GetOwnedNFTIds(string ownerAddress)
    {
        List<OwnedNFTItem> allItems = new List<OwnedNFTItem>();

        int limit = 50;

        for (int offset = 0; offset < int.MaxValue; offset += limit)
        {
            string result = await this.client.GetStringAsync(baseUri + "query/cirrus/collectables/" + ownerAddress + "?offset=" + offset + "&limit=" + limit);
            OwnedNFTIdsRoot root = JsonConvert.DeserializeObject<OwnedNFTIdsRoot>(result);
            allItems.AddRange(root.items);

            if (root.total < offset + limit)
                break;
        }

        return allItems;
    }

    public class OwnedNFTItem
    {
        public long id { get; set; }
        public string creator { get; set; }
        public string uri { get; set; }
        public bool isBurned { get; set; }
        public object pricePaid { get; set; }
        public string transactionId { get; set; }
        public string contractId { get; set; }
    }

    public class OwnedNFTIdsRoot
    {
        public int offset { get; set; }
        public int limit { get; set; }
        public int total { get; set; }
        public List<OwnedNFTItem> items { get; set; }
    }
}
