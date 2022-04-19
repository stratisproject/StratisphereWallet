using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class DisplayAnimationWIndow : WindowBase
{
    public Button Close;
    public Text InfoText;

    public RawImage Image;

    public VideoPlayer Player;

    public async void Awake()
    {
        this.Close.onClick.AddListener(async delegate { await this.HideAsync(); });
    }

    public async UniTask ShowPopupAsync(string resourceUri, string info)
    {
        string convertedAnimationUri = resourceUri;
        Image.gameObject.SetActive(false);

        await this.ShowAsync(false);

        InfoText.text = string.Empty;

        if (!resourceUri.EndsWith(".mp4"))
        {
            InfoText.text = "Downloading and converting animation to webm...";
            
            IDictionary<string, string> result = await MediaConverterManager.Instance.ConvertLinksAsync(new List<string>() {resourceUri});

            if (!result.TryGetValue(resourceUri, out convertedAnimationUri))
            {
                InfoText.text = "Conversion failed.";
                return;
            }
        }

       
        Player.source = VideoSource.Url;
        Player.url = convertedAnimationUri;
        Player.Prepare();
        Player.Play();
        InfoText.text = info;
        Image.gameObject.SetActive(true);
        Debug.Log(string.Format("Converted {0} to {1}", resourceUri, convertedAnimationUri));
    }

    public override UniTask HideAsync()
    {
        Image.gameObject.SetActive(false);
        return base.HideAsync();
    }
}
