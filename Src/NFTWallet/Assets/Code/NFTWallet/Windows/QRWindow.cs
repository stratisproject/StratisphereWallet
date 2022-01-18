using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZXing;
using ZXing.QrCode;

public class QRWindow : WindowBase
{
    public Button CloseButton;

    public Image QRImage;
    
    public async void Awake()
    {
        this.CloseButton.onClick.AddListener(async delegate { await this.HideAsync(); });
    }

    public async UniTask ShowPopupAsync(string dataToEncodeInQR)
    {
        Texture2D texture = GenerateQR(dataToEncodeInQR, 256);

        Sprite sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 50.0f);

        QRImage.sprite = sprite;

        await this.ShowAsync(false);
    }

    private Color32[] Encode(string textForEncoding, int width, int height)
    {
        var writer = new BarcodeWriter
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Height = height,
                Width = width
            }
        };
        return writer.Write(textForEncoding);
    }

    private Texture2D GenerateQR(string text, int sizePx)
    {
        var encoded = new Texture2D(sizePx, sizePx);
        var color32 = Encode(text, encoded.width, encoded.height);
        encoded.SetPixels32(color32);
        encoded.Apply();
        return encoded;
    }
}
