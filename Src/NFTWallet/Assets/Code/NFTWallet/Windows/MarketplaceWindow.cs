using System;
using System.Collections;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using ZXing;

public class MarketplaceWindow : WindowBase
{
    public Button ScanQRButton;

    public Text ScanQrButtonText;

    public RawImage Image;

    private WebCamTexture webcamTexture;

    private string QrCode = string.Empty;

    private bool isScanning = false;

    async void Awake()
    {
        if (Application.platform == RuntimePlatform.IPhonePlayer)
            this.Image.transform.Rotate(0, 180, 180);

        Image.gameObject.SetActive(false);
        ScanQrButtonText.text = "Scan QR";

        ScanQRButton.onClick.AddListener(async () =>
        {
            if (!isScanning)
            {
                isScanning = true;
                ScanQrButtonText.text = "Cancel";
                Debug.Log("SCAN QR PRESSED");

                QrCode = string.Empty;
                Image.gameObject.SetActive(true);
                StartCoroutine(GetQRCode());
            }
            else
            {
                isScanning = false;
                ScanQrButtonText.text = "Scan QR";
                Image.gameObject.SetActive(false);
            }
        });
    }

    private IEnumerator GetQRCode()
    {
        if (webcamTexture == null) {
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            if (Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                webcamTexture = new WebCamTexture(512, 512);
                Image.texture = webcamTexture;
            } else {
                yield break;
            }
        }

        IBarcodeReader barCodeReader = new BarcodeReader();
        webcamTexture.Play();
        Texture2D snap = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.ARGB32, false);

        while (string.IsNullOrEmpty(QrCode))
        {
            try
            {
                snap.SetPixels32(webcamTexture.GetPixels32());
                Result result = barCodeReader.Decode(snap.GetRawTextureData(), webcamTexture.width, webcamTexture.height, RGBLuminanceSource.BitmapFormat.ARGB32);
                if (result != null)
                {
                    QrCode = result.Text;
                    if (!string.IsNullOrEmpty(QrCode))
                    {
                        Debug.Log("DECODED TEXT FROM QR: " + QrCode);
                        break;
                    }
                }
            }
            catch (Exception ex) { Debug.LogWarning(ex.Message); }
            yield return null;
        }
        webcamTexture.Stop();

        Image.gameObject.SetActive(false);
        isScanning = false;
        ScanQrButtonText.text = "Scan QR";

        QRCodeScannedAsync(QrCode).GetAwaiter().GetResult();
    }

    private async UniTask QRCodeScannedAsync(string qrCode)
    {
        Debug.Log("QR data: " + qrCode);

        bool validExecutionRequest = false;
        MarketplaceRequestModel marketplaceRequestModel = null;

        try
        {
            marketplaceRequestModel = JsonConvert.DeserializeObject<MarketplaceRequestModel>(qrCode);
            validExecutionRequest = true;
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }

        try
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Sender: " + marketplaceRequestModel.sender);
            builder.AppendLine("To: " + marketplaceRequestModel.to);
            builder.AppendLine("Amount: " + marketplaceRequestModel.amount);
            builder.AppendLine("Method: " + marketplaceRequestModel.method);

            builder.AppendLine();
            builder.AppendLine("Parameters:");

            foreach (Parameter parameter in marketplaceRequestModel.parameters)
                builder.AppendLine(parameter.label + ": " + parameter.value);

            await NFTWalletWindowManager.Instance.PopupWindowYesNo.ShowPopupAsync(builder.ToString(), "Send transaction", async delegate
            {
                await MarketplaceIntegration.Instance.ExecuteMarketplaceRequestAsync(marketplaceRequestModel);
            });

            if (!validExecutionRequest)
            {
                // Try login
                Debug.Log("Logging in");
                HttpStatusCode status = await MarketplaceIntegration.Instance.LogInToNFTMarketplaceAsync(qrCode);

                string displayResult = status == HttpStatusCode.OK ? "Logged in successfully" : "Error while logging in";
                await NFTWalletWindowManager.Instance.PopupWindow.ShowPopupAsync(displayResult, "Login");
            }
        }
        catch (Exception e)
        {
            await NFTWalletWindowManager.Instance.PopupWindow.ShowPopupAsync(e.ToString(), "Error");
        }
    }
}
