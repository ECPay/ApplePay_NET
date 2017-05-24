using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Services;
using System.Web.Script.Serialization;
using System.Security.Cryptography.X509Certificates;
using System.Configuration;

public partial class ApplePaySample_V1 : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
          //  Common.CreateLogFile("ApplePaySample_V1", "Test"); ;
        }
    }

    [WebMethod]
    public static object validateMerchant(string validationURL)
    {
        string strResult = string.Empty;
        try
        {
            

            /* Merchant Identity憑證 */
            string certPath = HttpContext.Current.Server.MapPath(@"merchant_id_privateKey.p12"); //Merchant Identifier憑證路徑
            string certPwd = "ecpay";
            X509Certificate2 cert = new X509Certificate2(certPath, certPwd, X509KeyStorageFlags.MachineKeySet);

            /* 建立PayLoad */
            var payload = new
            {
                merchantIdentifier = "merchant.ECpay.ECC", //Your Merchant Identifie
                domainName = "applepay-stage.ecpay.com.tw", //Your Domain Name
                displayName = "ecpay" //Your Display Name
            };

            string strPayLoad = new JavaScriptSerializer().Serialize(payload);

            /* 將Payload以POST方式拋送至Apple提供的validationURL */
            /* HTTP Request需以Merchant Identity憑證送出 */
            /* 驗證成功後，Apple將會回傳Merchant Session物件*/
            #region HTTP Web Result
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(validationURL);
            request.Method = WebRequestMethods.Http.Post;
            request.ContentType = "application/json";
            request.ContentLength = strPayLoad.Length;

            request.ClientCertificates.Add(cert);

            using (StreamWriter sw = new StreamWriter(request.GetRequestStream()))
            {
                sw.Write(strPayLoad);
                sw.Flush();
                sw.Close();
            }

            HttpWebResponse response = request.GetResponse() as HttpWebResponse;

            using (StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                strResult = sr.ReadToEnd();
                sr.Close();
            }
            #endregion
        }
        catch (Exception ex)
        {
          
        }
        finally
        {
            
        }

        /* 將Merchant Session物件回應至Client端*/
        return new JavaScriptSerializer().DeserializeObject(strResult);
    }

    [WebMethod]
    public static object paymentProcess(string TradeAmount, string payment)
    {
        DateTime TradeDate = DateTime.Now;

        #region 執行ECPay的Apple Pay授權API

        //設定訂單交易參數
        string _MerchantID = "2000132"; //綠界提供給您的特店編號
        string _MerchantTradeNo = TradeDate.ToString("yyyyMMddHHmmssfff"); //您此筆訂單交易編號
        string _MerchantTradeDate = TradeDate.ToString("yyyy/MM/dd HH:mm:ss"); //您此筆訂單的交易時間
        string _TradeAmount = TradeAmount; //您此筆訂單的交易總金額
        string _CurrencyCode = "TWD";
        string _ItemName = "手機20元X2#隨身碟60元X1";  //您該筆商品的描述
        string _PlatformID = string.Empty;
        string _TradeDesc = "ecpay商城購物"; //您該筆訂單的描述

        #region 產生檢查碼
        string RealMerchantID = (string.IsNullOrEmpty(_PlatformID) ? _MerchantID : _PlatformID);

        string HashKey = ConfigurationManager.AppSettings[string.Format("{0}_HashKey", RealMerchantID)].ToString(); //綠界提供給您的Hash Key
        string HashIV = ConfigurationManager.AppSettings[string.Format("{0}_HashIV", RealMerchantID)].ToString(); //綠界提供給您的Hash IV

        Dictionary<string, string> postData = new Dictionary<string, string>();
        postData.Add("MerchantID", _MerchantID);
        postData.Add("MerchantTradeNo", _MerchantTradeNo);
        postData.Add("MerchantTradeDate", _MerchantTradeDate);
        postData.Add("TotalAmount", _TradeAmount);
        postData.Add("currencyCode", _CurrencyCode);
        postData.Add("ItemName", _ItemName);
        postData.Add("PlatformID", _PlatformID);
        postData.Add("TradeDesc", _TradeDesc);
        string _CheckMacValue = Common.GetCheckMacValue(postData, HashKey, HashIV);

        postData.Add("CheckMacValue", _CheckMacValue);
        #endregion

        #region 送出授權
        //PaymentToken進行AES加密，此欄位不加入檢查碼計算
        string _PaymentToken = Common.AES_Encrypt(payment, HashKey, HashIV);
        _PaymentToken = HttpUtility.UrlEncode(_PaymentToken);
        postData.Add("PaymentToken", _PaymentToken);

        //DoRequest
        string strPost = string.Empty, receiveData = string.Empty, requestUrl = string.Empty, ReturnData = string.Empty;

        foreach (KeyValuePair<string, string> kvp in postData)
        {
            if (strPost.Equals(string.Empty))
                strPost = string.Format("{0}={1}", kvp.Key, kvp.Value);
            else
                strPost += string.Format("&{0}={1}", kvp.Key, kvp.Value);
        }

        requestUrl = "https://Payment-stage.ecpay.com.tw/ApplePay/CreateServerOrder/V1"; //您要呼叫的服務位址

        try
        {
          
            receiveData = Common.SendRequest(requestUrl, strPost, "application/x-www-form-urlencoded", 0);
        }
        catch (Exception ex)
        {
         
            receiveData = string.Format(@"{{ ""RtnCode"":""0"",""RtnMsg"":""{0}"" }}", ex.Message);
        }
        finally
        {
          
            ReturnData = string.Format(@"{{ ""ReturnData"":{0} }}", receiveData);
        }


        #endregion

        #endregion

        /* 將授權結果回應至Client端 */
        return new JavaScriptSerializer().DeserializeObject(ReturnData);
    }
}