<%@ Page Language="C#" AutoEventWireup="true" CodeFile="ApplePaySample_V2.aspx.cs" Inherits="ApplePaySample_V2" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
<meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
    <title></title>
    <!-- Apple Pay Button Style -->
    <!-- 樣式說明: https://developer.apple.com/apple-pay/web-human-interface-guidelines/ -->
    <style type="text/css">
        @supports (-webkit-appearance: -apple-pay-button) {
            .apple-pay-button {
                display: inline-block;
                -webkit-appearance: -apple-pay-button;
            }

            .apple-pay-button-black {
                -apple-pay-button-style: black;
            }

            .apple-pay-button-white {
                -apple-pay-button-style: white;
            }

            .apple-pay-button-white-with-line {
                -apple-pay-button-style: white-outline;
            }
        }

        @supports not (-webkit-appearance: -apple-pay-button) {
            .apple-pay-button {
                display: inline-block;
                background-size: 100% 60%;
                background-repeat: no-repeat;
                background-position: 50% 50%;
                border-radius: 5px;
                padding: 0px;
                box-sizing: border-box;
                min-width: 200px;
                min-height: 32px;
                max-height: 64px;
            }

            .apple-pay-button-black {
                background-image: -webkit-named-image(apple-pay-logo-white);
                background-color: black;
            }

            .apple-pay-button-white {
                background-image: -webkit-named-image(apple-pay-logo-black);
                background-color: white;
            }

            .apple-pay-button-white-with-line {
                background-image: -webkit-named-image(apple-pay-logo-black);
                background-color: white;
                border: .5px solid black;
            }
        }
    </style>
    <script src="/scripts/jquery-1.11.1.min.js" type="text/javascript"></script>
    <script type="text/javascript">
        $(function () {
            /* 檢查當前瀏覽器是否可支援Apple Pay */
            if (window.ApplePaySession) {
                var merchantIdentifier = 'merchant.ECpay.ECC';  //請填入你申請的Apple Pay Merchant Identifier
                /* reference: https://developer.apple.com/reference/applepayjs/applepaysession/1778027-canmakepayments */
                /* 進行付款驗證設備是否能夠支援Apple Pay付款，不驗證用戶在電子錢包中是否有任何一張卡 */
                if (ApplePaySession.canMakePayments()) {
                    /* 進行付款驗證設備是否能夠支援Apple Pay付款且用戶在電子錢包中必需綁定一張卡 */
                    /* Safari設定中的「檢查ApplePay設定」關閉時canMakePayments一律回傳True */
                    /* reference: https://developer.apple.com/reference/applepayjs/applepaysession/1778000-canmakepaymentswithactivecard */
                    var promise = ApplePaySession.canMakePaymentsWithActiveCard(merchantIdentifier);
                    promise.then(function (canMakePayments) {
                        if (canMakePayments) {
                            $("#btnApplePay").click(beginPayment)
                            $("#divPay").show();
                        }
                        else {
                            $("#divPaymentData").html("<span style='font-size: 22px; font-weight: bold;'>目前無法使用Apple Pay</span>");
                        }
                    });
                }
                else {
                    $("#divPaymentData").html("<span style='font-size: 22px; font-weight: bold;'>您的裝置不支援Apple Pay</span>");
                }
            }
            else {
                /* 無法支援Apple Pay的相關處理 */
                $("#divPaymentData").html("<span style='font-size: 22px; font-weight: bold;'>您使用的瀏覽器不支援Apple Pay</span>");
            }
        });


        function beginPayment() {
            /* 建立 PaymentRequest */
            /* reference: https://developer.apple.com/reference/applepayjs/paymentrequest */
            var request = {
                countryCode: 'TW',
                currencyCode: 'TWD',
                supportedNetworks: ['visa', 'masterCard'],
                merchantCapabilities: ['supports3DS'],
                lineItems: [{ label: 'Test Goodies', amount: $("#TradeAmount").val() }],
                total: { label: 'ECPay Store', amount: $("#TradeAmount").val() }
            };

            /* 建立 ApplePaySession */
            /* reference: https://developer.apple.com/reference/applepayjs/applepaysession/2320659-applepaysession */
            var session = new ApplePaySession(2, request);
            /* 商店驗證事件 */
            session.onvalidatemerchant = function (event) {
                var data = {
                    validationURL: event.validationURL
                };
                /* 將validationURL拋到Server端，由Server端與Apple Server做商店驗證 */
                $.ajax({
                    url: "ApplePaySample_V2.aspx/validateMerchant",
                    method: "POST",
                    contentType: "application/json; charset=utf-8",
                    data: JSON.stringify(data),
                    error: function (err) {
                        console.log(err);
                    }
                }).then(function (merchantSession) {
                    /* 後端驗證成功取得Merchant Session物件後，將物件pass給ApplePaySession */
                    session.completeMerchantValidation(merchantSession.d);
                });
            };

            session.onpaymentmethodselected = function (event) {
                var newTotal = { type: 'final', label: 'ECPay Store', amount: $("#TradeAmount").val() };
                var newLineItems = [{ type: 'final', label: 'Test Goodies', amount: $("#TradeAmount").val() }];
                session.completePaymentMethodSelection(newTotal, newLineItems);
            }

            /* 付款授權事件 */
            session.onpaymentauthorized = function (event) {
                var data = {
                    TradeAmount: $("#TradeAmount").val(),
                    payment: JSON.stringify(event.payment)
                };
                /* 將payment物件拋至Server端，由Server端處理交易授權 */
                $.ajax({
                    url: "ApplePaySample_V2.aspx/paymentProcess",
                    method: "POST",
                    contentType: "application/json; charset=utf-8",
                    data: JSON.stringify(data),
                    error: function (err) {
                        console.log(err);
                    }
                }).then(function (result) {
                    /* 依授權結果決定帶入ApplePaySession的回應 */
                    /* competePayment reference: https://developer.apple.com/reference/applepayjs/applepaysession/1778012-completepayment */
                    if (result.d.ReturnData.RtnCode == "1") {
                        $("#divRtnMsg").html("<pre>交易完成</pre>");
                        session.completePayment(ApplePaySession.STATUS_SUCCESS);
                    }
                    else {
                        $("#divRtnMsg").html("<pre>" + result.d.ReturnData.RtnMsg + "</pre>");
                        session.completePayment(ApplePaySession.STATUS_FAILURE);
                    }
                    $("#divPaymentData").html("<pre>" + JSON.stringify(result.d, undefined, 2) + "</pre>");
                });
            }

            session.oncancel = function (event) {

            }

            try {
                session.begin();
            } catch (e) {
                alert(JSON.stringify(e));
                return false;
            }

            return false;
        }
    </script>
</head>
<body>
    <form id="form1" runat="server">
        <p>
            <div id="divPay" style="display:none">
                交易金額：
                <input id="TradeAmount" type="tel" value="100" step="1"/>
                <br />
                <br />
                <button id="btnApplePay" class="apple-pay-button apple-pay-button-white" style="-webkit-appearance: -apple-pay-button; -apple-pay-button-type: buy; width: 400px; height: 64px;"></button>
            </div>
        </p>
        <p id="divLog" align="left" style="font-size: 26px;"></p>
        <p id="divRtnMsg" align="left" style="font-size: 26px;"></p>
        <p id="divPaymentData" align="left" style="font-size: 26px;"></p>
    </form>
</body>
</html>
