using System.Net;
using HtmlAgilityPack;

public class TranslationTool {

    private const string URL = "https://www.google.com/translate_t?hl=en&ie=UTF8&text={0}&langpair=ja|en";

    public static string GetPhoneticReading(string text) {
        HtmlDocument doc = new HtmlWeb().Load(string.Format(URL, text));
        return WebUtility.HtmlDecode(doc.GetElementbyId("src-translit").InnerText);
    }

    public static string GetTranslation(string text) {
        HtmlDocument doc = new HtmlWeb().Load(string.Format(URL, text));
        return WebUtility.HtmlDecode(doc.GetElementbyId("result_box").InnerText);
    }

}
