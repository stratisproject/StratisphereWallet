using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
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

    async void Start()
    {
        webcamTexture = new WebCamTexture(512, 512);
        Image.texture = webcamTexture;
    }

    private IEnumerator GetQRCode()
    {
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


        Task.Run(async () =>
        {
            try
            {
                await QRCodeScannedAsync(QrCode);
            }
            catch (Exception e)
            {
                 Debug.LogError(e);
            }
        });
    }

    private async Task QRCodeScannedAsync(string qrCode)
    {
        if (qrCode.Contains("callback"))
        {
            Debug.Log("Logging in");
            await MarketplaceIntegration.Instance.LogInToNFTMarketplaceAsync(qrCode);
        }
        else
        {
            Debug.Log("Executing request: " + qrCode);

            MarketplaceRequestModel model = JsonConvert.DeserializeObject<MarketplaceRequestModel>(qrCode);

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Sender: " + model.sender);
            builder.AppendLine("To: " + model.to);
            builder.AppendLine("Amount: " + model.amount);
            builder.AppendLine("Method: " + model.method);

            builder.AppendLine();
            builder.AppendLine("Parameters:");

            foreach (Parameter parameter in model.parameters)
                builder.AppendLine(parameter.label + ": " + parameter.value);

            await NFTWalletWindowManager.Instance.PopupWindowYesNo.ShowPopupAsync(builder.ToString(), "Send transaction", async delegate
            {
                await MarketplaceIntegration.Instance.ExecuteMarketplaceRequestAsync(model);
            });
        }
    }
}
