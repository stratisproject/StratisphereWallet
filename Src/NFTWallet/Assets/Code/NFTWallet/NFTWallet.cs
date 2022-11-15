using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using MediaConverterApi;
using NBitcoin;
using Newtonsoft.Json;
using Pinata.Client;
using Stratis.Sidechains.Networks;
using StratisNodeApi;
using UnityEngine;
using Network = NBitcoin.Network;

public class NFTWallet : MonoBehaviour
{
    public static NFTWallet Instance;

    public TargetNetwork DefaultNetwork = TargetNetwork.CirrusMain;

    public TargetNetwork CurrentNetwork => currentNetwork;

    // Test: https://api-sfn-test.stratisphere.com
    // Main: https://api-sfn.stratisphere.com
    public string TestnetApiUrl = "https://api-sfn-test.stratisphere.com/"; //http://localhost:44336/
    public string MainnetApiUrl = "https://api-sfn.stratisphere.com/";

    public string MainnetBlockCoreIndexerApi = "https://crs.indexer.thedude.pro/api/";

    public string TestnetBlockCoreIndexerApi = "https://tcrs.indexer.blockcore.net/api/";

    public string MediaConversionApiUrl = "http://148.251.15.126:7110/";

    public string Pinata_APIKey = "771ae30ada3e6674cfbf";

    public string Pinata_APISecret = "9c710214ec7220e84f625455d2b5f9fa44152928128572bc1be7b3af91770de8";

    public string UnityApiUrl => CurrentNetwork == TargetNetwork.CirrusMain ? MainnetApiUrl : TestnetApiUrl;

    public string BlockCoreApiUrl => CurrentNetwork == TargetNetwork.CirrusMain ? MainnetBlockCoreIndexerApi : TestnetBlockCoreIndexerApi;

    public Network Network => network;

    public bool DEBUG_ResetMnemonicAtStart = false;

    private Network network;

    [HideInInspector]
    public StratisUnityManager StratisUnityManager;

    [HideInInspector]
    public PinataClient PinataClient;

    public GameObject[] MobileSpecific;
    public GameObject[] StandaloneSpecific;

    private string WatchedNFTsKey => CurrentNetwork == TargetNetwork.CirrusMain ? "watchedNFTs_main" : "watchedNFTs_test";

    private TargetNetwork currentNetwork;

    private const string MnemonicKey = "MnemonicST";

    void Awake()
    {
        this.PinataClient = new PinataClient(new Config()
        {
            ApiKey = this.Pinata_APIKey,
            ApiSecret = this.Pinata_APISecret
        });

        if (DEBUG_ResetMnemonicAtStart)
            DeleteMnemonic();

        Instance = this;

        MediaConverterManager manager = new MediaConverterManager(new MediaConverterClient(MediaConversionApiUrl));

        bool enableMobile = false;
        bool enableStandalone = false;

        #if UNITY_ANDROID || UNITY_IPHONE
                enableMobile = true;
        #else
                   enableStandalone = true;
        #endif

        foreach (GameObject o in MobileSpecific)
            o.SetActive(enableMobile);

        foreach (GameObject o in StandaloneSpecific)
            o.SetActive(enableStandalone);
    }

    public BlockCoreApi GetBlockCoreApi()
    {
        return new BlockCoreApi(BlockCoreApiUrl);
    }

    /// <returns><c>true</c> if success.</returns>
    public async UniTask<bool> InitializeAsync(string mnemonic, TargetNetwork initNetwork, string passphrase = null)
    {
        Debug.Log("Initializing on " + initNetwork);

        this.currentNetwork = initNetwork;

        if (CurrentNetwork == TargetNetwork.CirrusTest)
            network = new CirrusTest();
        else
            network = new CirrusMain();

        try
        {
            Wordlist wordlist = Wordlist.AutoDetect(mnemonic);

            this.StratisUnityManager = new StratisUnityManager(new StratisNodeClient(UnityApiUrl), Network,
                new Mnemonic(mnemonic, wordlist), passphrase);

            await this.StratisUnityManager.GetBalanceAsync();
        }
        catch (SocketException e)
        {
            Debug.LogError(e);
            return false;
        }
        catch (HttpRequestException e)
        {
            Debug.LogError(e);
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return false;
        }

        Debug.Log("Initialized");
        return true;
    }

    public async UniTask AddKnownContractsIfMissingAsync()
    {
        try
        {
            List<BlockCoreApi.OwnedNFTItem> ownedNfts = await this.GetBlockCoreApi().GetOwnedNFTIds(this.StratisUnityManager.GetAddress().ToString());

            List<string> contracts = ownedNfts.Select(x => x.contractId).Distinct().ToList();

            var loaded = LoadKnownNfts().Select(x => x.ContractAddress);

            var contractsToAdd = contracts.Where(x => !loaded.Contains(x));

            foreach (string contractToAdd in contractsToAdd)
            {
                bool success = await RegisterKnownNFTAsync(contractToAdd);

                Debug.Log(string.Format("Contract {0} registered. Success: {1}", contractToAdd, success));
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    public async UniTask<bool> RegisterKnownNFTAsync(string contractAddress)
    {
        NFTWrapper wrapper = new NFTWrapper(NFTWallet.Instance.StratisUnityManager, contractAddress);

        string nftname, ownerAddr, symbol;

        try
        {
            nftname = await wrapper.NameAsync();
            ownerAddr = await wrapper.OwnerAsync();
            symbol = await wrapper.SymbolAsync();
        }
        catch (Exception e)
        {
            return false;
        }

        await NFTWallet.Instance.RegisterKnownNFTAsync(nftname, symbol, null, contractAddress, ownerAddr);

        return true;
    }

    public async UniTask RegisterKnownNFTAsync(string nftName, string symbol, bool? ownerOnlyMinting, string contractAddress, string ownerAddress)
    {
        List<DeployedNFTModel> knownNfts = LoadKnownNfts();

        //try
        //{
        //    await this.StratisUnityManager.Client.WatchNftContractAsync(contractAddress);
        //}
        //catch (Exception e)
        //{
        //    Debug.Log("Cant watch nft contracts...");
        //}

        if (knownNfts.Any(x => x.ContractAddress == contractAddress))
            return;

        knownNfts.Add(new DeployedNFTModel()
        {
            ContractAddress = contractAddress,
            NftName = nftName,
            OwnerOnlyMinting = ownerOnlyMinting,
            Symbol = symbol,
            OwnerAddress = ownerAddress
        });

        this.PersistKnownNfts(knownNfts);
    }

    public List<DeployedNFTModel> LoadKnownNfts()
    {
        if (!PlayerPrefs.HasKey(WatchedNFTsKey))
            return new List<DeployedNFTModel>();

        string json = PlayerPrefs.GetString(WatchedNFTsKey);

        List<DeployedNFTModel> nfts = JsonConvert.DeserializeObject<List<DeployedNFTModel>>(json);

        return nfts;
    }

    public void PersistKnownNfts(List<DeployedNFTModel> knownNfts)
    {
        string json = JsonConvert.SerializeObject(knownNfts);
        PlayerPrefs.SetString(WatchedNFTsKey, json);
    }

    public bool IsMnemonicSaved()
    {
        return PlayerPrefs.HasKey(MnemonicKey);
    }

    public string GetSavedMnemonic()
    {
        string mnemonic = PlayerPrefs.GetString(MnemonicKey);

        return mnemonic;
    }

    public void SaveMnemonic(string mnemonic)
    {
        PlayerPrefs.SetString(MnemonicKey, mnemonic);
    }

    public void DeleteMnemonic()
    {
        PlayerPrefs.DeleteKey(MnemonicKey);
    }
}


public enum TargetNetwork
{
    CirrusTest = 0,
    CirrusMain = 1
}