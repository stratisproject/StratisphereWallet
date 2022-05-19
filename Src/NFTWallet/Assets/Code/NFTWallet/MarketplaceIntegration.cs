using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NBitcoin;
using Newtonsoft.Json;
using Unity3dApi;
using UnityEngine;

public class MarketplaceIntegration : MonoBehaviour
{
    public string MarketplaceURI_Testnet = "https://nftmarketplacetest.azurewebsites.net/";

    public string MarketplaceURI_Mainnet = "https://nftmarketplace.azurewebsites.net/";

    public string MarketplaceURI => NFTWallet.Instance.CurrentNetwork == TargetNetwork.CirrusMain ? MarketplaceURI_Mainnet : MarketplaceURI_Testnet;

    public string  ApiUri => MarketplaceURI + "api/";

    public static MarketplaceIntegration Instance;

    private HttpClient client = new HttpClient();

    async void Awake()
    {
        Instance = this;
    }

    /// <summary>Returns link to .json or to image if json upload fails</summary>
    public async UniTask<string> UploadMetadataAsync(NFTMetadataModel metadata)
    {
        string jsonString = JsonConvert.SerializeObject(metadata);

        StringContent stringContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
        HttpResponseMessage callbackResult = await client.PostAsync(ApiUri + "contract/metadata", stringContent);

        string result = await callbackResult.Content.ReadAsStringAsync();

        if (result.Length > 1000 || !result.EndsWith(".json"))
        {
            Debug.Log("No json metadata! " + result);
            result = metadata.Image;
        }

        Debug.Log(result);
        return result;
    }

    public async UniTask ExecuteMarketplaceRequestAsync(string transferData)
    {
        MarketplaceRequestModel model = JsonConvert.DeserializeObject<MarketplaceRequestModel>(transferData);
        string[] parameters = model.parameters.Select(x => x.value).ToArray();
        Money amount = new Money(Decimal.Parse(model.amount.ToString()), MoneyUnit.BTC);
        Task<string> sendTask = NFTWallet.Instance.StratisUnityManager.SendCallContractTransactionAsync(model.to, model.method, parameters, amount);

        // Call callback
        StringContent stringContent = new StringContent(string.Empty);
        HttpResponseMessage callbackResult = await client.PostAsync(model.callback, stringContent);

        ReceiptResponse receipt = await NFTWalletWindowManager.Instance.WaitTransactionWindow.DisplayUntilSCReceiptReadyAsync(sendTask);
        bool success = receipt.Success;
        string resultString = string.Format("Marketplace request execution success: {0}", success);
        await NFTWalletWindowManager.Instance.PopupWindow.ShowPopupAsync(resultString, "EXECUTE MARKETPLACE REQUEST");
    }

    // TODO check if expired
    public async UniTask LogInToNFTMarketplaceAsync(string loginData)
    {
        //string testData = "sid:nftmarketplacetest.azurewebsites.net/api/login/login-callback?uid=CBN3hhkyh3ddkl98JTa4e3zn1yohMnQScs7UuG-Ok9m7uupyF4z9eM9UhBw1qOTP&exp=1642262570";

        QRDataParseResult parsed = this.ParseLoginData(loginData);

        // Sign
        StratisSignatureAuthCallbackBody signed = SignCallback(parsed.CallbackURI);

        // Send
        string jsonString = JsonConvert.SerializeObject(signed);

        Debug.Log(jsonString);

        StringContent stringContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
        HttpResponseMessage callbackResult = await client.PostAsync("https://" + parsed.CallbackURI, stringContent);

        Debug.Log(callbackResult);
    }

    private async UniTask<QRDataParseResult> CallApiRequestLoginAsync()
    {
        HttpResponseMessage result = await client.GetAsync(ApiUri + "login");
        string content = await result.Content.ReadAsStringAsync();

        LoginRequestModel model = JsonConvert.DeserializeObject<LoginRequestModel>(content);

        QRDataParseResult parsed = this.ParseLoginData(model.sid);

        return parsed;
    }

    public QRDataParseResult ParseLoginData(string data)
    {
        QRDataParseResult result = new QRDataParseResult();

        int expIndex = data.IndexOf("exp=");
        if (expIndex != -1)
        {
            string expirationStr = data.Substring(expIndex + 4);
            uint exp = uint.Parse(expirationStr);
            result.ExpirationDate = exp;
        }

        result.CallbackURI = data.Substring(4);

        return result;
    }

    public StratisSignatureAuthCallbackBody SignCallback(string callbackURI)
    {
        StratisSignatureAuthCallbackBody body = new StratisSignatureAuthCallbackBody()
        {
            PublicKey = NFTWallet.Instance.StratisUnityManager.GetAddress().ToString(),
            Signature = NFTWallet.Instance.StratisUnityManager.SignMessage(callbackURI)
        };

        return body;
    }

    public string GetSellURI(string contractAddress, long tokenId)
    {
        return MarketplaceURI + "asset/sale/list?contract=" + contractAddress + "&tokenId=" + tokenId;
    }
}

public class QRDataParseResult
{
    public string CallbackURI { get; set; }

    public uint ExpirationDate { get; set; }

    public bool IsExpired()
    {
        long unixTimeNow = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();

        return unixTimeNow > ExpirationDate;
    }
}

//https://github.com/Opdex/SSAS.NET/blob/main/src/SSAS.NET/StratisSignatureAuthCallbackBody.cs
/// <summary>
/// Callback body for Stratis Signature Auth Specification.
/// </summary>
public class StratisSignatureAuthCallbackBody
{
    /// <summary>
    /// Signed Stratis ID callback.
    /// </summary>
    /// <example>H9xjfnvqucCmi3sfEKUes0qL4mD9PrZ/al78+Ka440t6WH5Qh0AIgl5YlxPa2cyuXdwwDa2OYUWR/0ocL6jRZLc=</example>
    public string Signature { get; set; }

    /// <summary>
    /// Message signer wallet address.
    /// </summary>
    /// <example>tQ9RukZsB6bBsenHnGSo1q69CJzWGnxohm</example>
    public string PublicKey { get; set; }
}

public class LoginRequestModel
{
    public string sid { get; set; }
}

// Sale models
public class Parameter
{
    public string label { get; set; }
    public string value { get; set; }
}

public class MarketplaceRequestModel
{
    public string eventId { get; set; }
    public string sender { get; set; }
    public string to { get; set; }
    public decimal amount { get; set; }
    public string method { get; set; }
    public List<Parameter> parameters { get; set; }
    public string callback { get; set; }
}