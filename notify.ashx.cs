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
        /// Thsi processing may vary widely between banks.
        /// </summary>
        /// <param name="context"></param>
        public void ProcessRequest(HttpContext context)
        {
            var modCtrl = new NBrightBuyController();
            var info = ProviderUtils.GetProviderSettings("NBrightPayPalpayment");

            try
            {
                var ipn = new PayPalIpnParameters(context.Request.Form);

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
                        if (ProviderUtils.VerifyPayment(ipn, validateUrl))
                        {

                            if (debugMode)
                            {
                                info.SetXmlProperty("genxml/debugmsg", debugMsg);
                                modCtrl.Update(info);
                            }

                            //check that the order is valid. (Not yet been processed  "020" = Waiting for Bank) 
                            if (orderData.OrderStatus == "020")
                            {
                                //set order status to Payed
                                orderData.PaymentOk();
                            }
                        }
                        else
                        {
                            if (ipn.IsValid)
                            {
                                info.SetXmlProperty("genxml/debugmsg", "NOT VALIDATED BY PAYPAL");                               
                            }
                            else
                            {
                                info.SetXmlProperty("genxml/debugmsg", "PAYMENT FAIL");                                             
                            }
                            orderData.PaymentFail();
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