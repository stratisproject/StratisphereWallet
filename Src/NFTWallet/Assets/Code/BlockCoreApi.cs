using System;
using System.Collections.Generic;
using System.Linq;
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

    public async Task<List<UTXOModel>> GetUTXOsAsync(string address)
    {
        List<UTXOModel> allItems = new List<UTXOModel>();

        int limit = 50;

        for (int offset = 0; offset < int.MaxValue; offset += limit)
        {
            string endpoint = baseUri + "query/address/" + address + "/transactions/unspent?confirmations=1&offset=" + offset + "&limit=" + limit;

            HttpResponseMessage response = await client.GetAsync(endpoint);

            var totalString = response.Headers.GetValues("pagination-total").First();
            int total = int.Parse(totalString);

            string result = await response.Content.ReadAsStringAsync();
            List<RootUtxos> rootCollection = JsonConvert.DeserializeObject<List<RootUtxos>>(result);

            foreach (RootUtxos utxoData in rootCollection)
            {
                allItems.Add(new UTXOModel()
                {
                    Hash = utxoData.outpoint.transactionId,
                    N = utxoData.outpoint.outputIndex,
                    Satoshis = utxoData.value
                });
            }
            
            if (total < offset + limit)
                break;
        }

        return allItems;
    }

    public async Task<long> GetBalanceAsync(string address)
    {
        string result = await this.client.GetStringAsync(baseUri + "query/address/" + address);
        AddressInfoModel root = JsonConvert.DeserializeObject<AddressInfoModel>(result);

        return root.balance;
    }

    #region GetOwnedNFTIds models
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
    #endregion

    #region utxos models
    public class UTXOModel
    {
        public string Hash { get; set; }

        public int N { get; set; }

        public long Satoshis { get; set; }
    }

    public class Outpoint
    {
        public string transactionId { get; set; }
        public int outputIndex { get; set; }
    }

    public class RootUtxos
    {
        public Outpoint outpoint { get; set; }
        public string address { get; set; }
        public string scriptHex { get; set; }
        public long value { get; set; }
        public int blockIndex { get; set; }
        public bool coinBase { get; set; }
        public bool coinStake { get; set; }
    }
    #endregion

    public class AddressInfoModel
    {
        public string address { get; set; }
        public long balance { get; set; }
        public long totalReceived { get; set; }
        public int totalStake { get; set; }
        public long totalMine { get; set; }
        public long totalSent { get; set; }
        public int totalReceivedCount { get; set; }
        public int totalSentCount { get; set; }
        public int totalStakeCount { get; set; }
        public int totalMineCount { get; set; }
        public int pendingSent { get; set; }
        public int pendingReceived { get; set; }
    }
}
