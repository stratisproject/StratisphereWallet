using System;
using System.Net.Http;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class MarketplaceIntegration : MonoBehaviour
{
    public string ApiURI = "https://nftmarketplacetest.azurewebsites.net/api/";

    public static MarketplaceIntegration Instance;

    private HttpClient client = new HttpClient();

    void Awake()
    {
        Instance = this;
    }

    // TODO check if expired
    public async UniTask LogInToNFTMarketplace(string loginData)
    {
        //string testData = "sid:nftmarketplacetest.azurewebsites.net/api/login/login-callback?uid=CBN3hhkyh3ddkl98JTa4e3zn1yohMnQScs7UuG-Ok9m7uupyF4z9eM9UhBw1qOTP&exp=1642262570";

        QRDataParseResult parsed = this.ParseLoginData(loginData);
        
        // Sign
        StratisSignatureAuthCallbackBody signed = SignCallback(parsed.CallbackURI);
        
        // Send
        string jsonString = JsonConvert.SerializeObject(signed);

        Debug.Log(jsonString);

        var stringContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
        var result = await client.PostAsync("https://" + parsed.CallbackURI, stringContent);

        Debug.Log(result);
    }

    private async UniTask<QRDataParseResult> CallApiRequestLoginAsync()
    {
        HttpResponseMessage result = await client.GetAsync(ApiURI + "login");
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
