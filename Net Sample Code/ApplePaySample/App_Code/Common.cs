using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Net.Security;
using System.IO;

/// <summary>
/// 共用函式
/// </summary>
public class Common
{
    
    /// <summary>
    /// AES加密 CBC模式
    /// </summary>
    /// <param name="plainText">待加密的字串</param>
    /// <param name="key">HashKey</param>
    /// <param name="iv">HashIV</param>
    /// <returns>Base64字串</returns>
    public static string AES_Encrypt(string plainText, string key, string iv)
    {
        RijndaelManaged aes = new RijndaelManaged();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV = Encoding.UTF8.GetBytes(iv);
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        byte[] bsFile = Encoding.UTF8.GetBytes(plainText);
        ICryptoTransform transform = aes.CreateEncryptor();
        byte[] outputData = transform.TransformFinalBlock(bsFile, 0, bsFile.Length);

        return Convert.ToBase64String(outputData);
    }


    /// <summary>
    /// SHA256加密
    /// </summary>
    /// <param name="str">待加密的字串</param>
    /// <returns>加密後的字串(十六進位 兩位數)</returns>
    public static string SHAEncrypt(string str)
    {
        System.Security.Cryptography.SHA256 sha256 = new System.Security.Cryptography.SHA256Managed();
        byte[] sha256Bytes = System.Text.Encoding.Default.GetBytes(str);
        byte[] cryString = sha256.ComputeHash(sha256Bytes);
        string sha256Str = string.Empty;
        for (int i = 0; i < cryString.Length; i++)
        {
            sha256Str += cryString[i].ToString("X2");
        }
        return sha256Str;
    }

    public static string GetCheckMacValue(Dictionary<string, string> postData, string HashKey, string HashIV)
    {

        Dictionary<string, string> postParameterList = new Dictionary<string, string>();

        var chkList = postData.OrderBy(x => x.Key); // 排序
        StringBuilder ChkParameter = new StringBuilder(); // 依英文字母順序排序, 前後加上HashKey及HashIV

        ChkParameter.AppendFormat("HashKey={0}", HashKey);
        foreach (var item in chkList)
        {
            ChkParameter.AppendFormat("&" + item.Key + "={0}", item.Value);
        }
        ChkParameter.AppendFormat("&HashIV={0}", HashIV);

        //URL Encode
        string Chkencode = HttpUtility.UrlEncode(ChkParameter.ToString());
        //轉小寫
        string ChklowerEncoded = Chkencode.ToLower();
        //檢查碼
        string ChkMacValue = SHAEncrypt(ChklowerEncoded);

        return ChkMacValue;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="certification"></param>
    /// <param name="chain"></param>
    /// <param name="sslPolicyErrors"></param>
    /// <returns></returns>
    public static bool   AcceptAllCertifications(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
    {
        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="requestUrl"></param>
    /// <param name="postData"></param>
    /// <param name="contentType"></param>
    /// <param name="TimeoutSeconds"></param>
    /// <returns></returns>
    public static string SendRequest(string requestUrl, string postData, string contentType = "application/x-www-form-urlencoded", int TimeoutSeconds = 0)
    {
        // 建立utf-8 Encoding
        Encoding encoding = Encoding.GetEncoding(65001);

        //### 建立HttpWebRequest物件
        HttpWebRequest httpWebRequest = null;

        //### 如果是https請求
        if (requestUrl.StartsWith("https", StringComparison.OrdinalIgnoreCase))
        {
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(AcceptAllCertifications);
            httpWebRequest = WebRequest.Create(requestUrl) as HttpWebRequest;
            httpWebRequest.ProtocolVersion = HttpVersion.Version10;
        }
        else
        {
            httpWebRequest = WebRequest.Create(requestUrl) as HttpWebRequest;
            httpWebRequest.ProtocolVersion = HttpVersion.Version11;
        }

        //### 指定送出去的方式為POST
        httpWebRequest.CookieContainer = new CookieContainer(); 
        httpWebRequest.Method = "POST";
        httpWebRequest.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; NeosBrowser; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";
        httpWebRequest.Accept = "text/html";
        httpWebRequest.Referer = "https://greenworld.com.tw";


        //### 設定要送出的參數; separated by "&"
        string data = postData;

        //### 設定content type, it is required, otherwise it will not work.
        httpWebRequest.ContentType = contentType;

        //### Timeout時間預設為10秒
        if (TimeoutSeconds > 0)
        {
            //設定Timeout時間(單位毫秒)
            httpWebRequest.Timeout = TimeoutSeconds * 1000;
        }

        //### 預設回傳的資料
        string receiveData = "Post Data Error";

        try
        {
            //### 取得request stream 並且寫入post data
            using (StreamWriter sw = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                sw.Write(data);
                sw.Flush();
                sw.Close();
            }

            //### 取得server的reponse結果
            HttpWebResponse httpWebResponse = httpWebRequest.GetResponse() as HttpWebResponse;
            using (StreamReader sr = new StreamReader(httpWebResponse.GetResponseStream(), encoding))
            {
                receiveData = sr.ReadToEnd();
                sr.Close();
            }
        }
        catch (Exception exception)
        {
            receiveData = exception.ToString();
        }

        return receiveData;
    }
}