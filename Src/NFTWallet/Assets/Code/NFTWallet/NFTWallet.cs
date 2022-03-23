using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using NBitcoin;
using Newtonsoft.Json;
using Stratis.Sidechains.Networks;
using Unity3dApi;
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

    public string ApiUrl => CurrentNetwork == TargetNetwork.CirrusMain ? MainnetApiUrl : TestnetApiUrl;

    public Network Network => network;

    private Network network;

    [HideInInspector]
    public StratisUnityManager StratisUnityManager;

    public GameObject[] MobileSpecific;
    public GameObject[] StandaloneSpecific;
    
    private string WatchedNFTsKey => CurrentNetwork == TargetNetwork.CirrusMain ? "watchedNFTs_main" : "watchedNFTs_test";

    private TargetNetwork currentNetwork;

    void Awake()
    {
        Instance = this;

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
    
    /// <returns><c>true</c> if success.</returns>
    public async UniTask<bool> InitializeAsync(string mnemonic, TargetNetwork initNetwork, string passphrase = null)
    {
        Debug.Log("Initializing: " + initNetwork.ToString());

        this.currentNetwork = initNetwork;

        if (CurrentNetwork == TargetNetwork.CirrusTest)
            network = new CirrusTest();
        else
            network = new CirrusMain();

        try
        {
            Wordlist wordlist = Wordlist.AutoDetect(mnemonic);

            this.StratisUnityManager = new StratisUnityManager(new Unity3dClient(ApiUrl), Network,
                new Mnemonic(mnemonic, wordlist), passphrase);
            
            await this.StratisUnityManager.GetBalanceAsync();
        }
        catch (SocketException e)
        {
            return false;
        }
        catch (HttpRequestException e)
        {
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return false;
        }

        return true;
    }

    public async UniTask AddKnownContractsIfMissingAsync()
    {
        try
        {
            OwnedNFTsModel ownedNfts = await this.StratisUnityManager.Client.GetOwnedNftsAsync(this.StratisUnityManager.GetAddress().ToString());

            var contracts = ownedNfts.OwnedIDsByContractAddress.Keys.ToList();

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

        await this.StratisUnityManager.Client.WatchNftContractAsync(contractAddress);

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
}


public enum TargetNetwork
{
    CirrusTest = 0, 
    CirrusMain = 1
}