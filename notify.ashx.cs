using System;
using System.Web;
using NBrightCore.common;
using Nevoweb.DNN.NBrightBuy.Components;

namespace Nevoweb.DNN.NBrightBuyPayPal
{
    /// <summary>
    /// Summary description for XMLconnector
    /// </summary>
    public class NBrightPayPalNotify : IHttpHandler
    {
        private String _lang = "";

        /// <summary>
        /// This function needs to process and returned message from the bank.
        /// This processing may vary widely between banks.
        /// </summary>
        /// <param name="context"></param>
        public void ProcessRequest(HttpContext context)
        {
            var modCtrl = new NBrightBuyController();
            var info = ProviderUtils.GetProviderSettings("NBrightPayPalpayment");

            try
            {
                var ipn = new PayPalIpnParameters(context.Request);

                var debugMode = info.GetXmlPropertyBool("genxml/checkbox/debug.mode");

                var debugMsg = "START CALL" + DateTime.Now.ToString("s") + " </br>";
                debugMsg += "returnmessage: " + context.Request.Form.Get("returnmessage") + "</br>";
                if (debugMode)
                {
                    info.SetXmlProperty("genxml/debugmsg", debugMsg);
                    modCtrl.Update(info);
                }

                debugMsg += "NBrightPayPal DEBUG: " + DateTime.Now.ToString("s") + " </br>" + context.Request.Form + "<br/>";

                if (Utils.IsNumeric(ipn.item_number))
                {

                    var validateUrl = info.GetXmlProperty("genxml/textbox/paymenturl") + "?" + ipn.PostString;

                    // check the record exists
                    debugMsg += "OrderId: " + ipn.item_number + " </br>";
                    var nbi = modCtrl.Get(Convert.ToInt32(ipn.item_number), "ORDER");
                    if (nbi != null)
                    {
                        var orderData = new OrderData(nbi.ItemID);
                        debugMsg += "validateUrl: " + validateUrl + " </br>";
                        if (ProviderUtils.VerifyPayment(ipn, validateUrl))
                        {
                            //set order status to Payed
                            debugMsg += "PaymentOK </br>";
                            orderData.PaymentOk();
                        }
                        else
                        {
                            if (ipn.IsValid)
                            {
                                debugMsg += "NOT VALIDATED BY PAYPAL </br>";
                                //set order status to Not verified
                                orderData.PaymentOk("050");
                            }
                            else
                            {
                                debugMsg += "PAYMENT FAIL </br>";
                                orderData.PaymentFail();
                            }
                        }
                    }
                    else
                    {
                        debugMsg += "ORDER does not exists";                        
                    }
                    if (debugMode)
                    {
                        info.SetXmlProperty("genxml/debugmsg", debugMsg);
                        modCtrl.Update(info);
                    }
                }

            }
            catch (Exception ex)
            {
                if (!ex.ToString().StartsWith("System.Threading.ThreadAbortException")) // we expect a thread abort from the End response.
                {
                    info.SetXmlProperty("genxml/debugmsg", "NBrightPayPal ERROR: " + ex.ToString());
                    modCtrl.Update(info);
                }
            }
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }


    }
}