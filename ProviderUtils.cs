using DotNetNuke.Entities.Portals;
using NBrightCore.common;
using NBrightDNN;
using Nevoweb.DNN.NBrightBuy.Components;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;

namespace Nevoweb.DNN.NBrightBuyPayPal
{
    public class ProviderUtils
    {

        public static String GetTemplateData(String templatename)
        {
            var controlMapPath = HttpContext.Current.Server.MapPath("/DesktopModules/NBright/NBrightPayPal");
            var templCtrl = new NBrightCore.TemplateEngine.TemplateGetter(PortalSettings.Current.HomeDirectoryMapPath, controlMapPath, "Themes\\config", "");
            var templ = templCtrl.GetTemplateData(templatename, Utils.GetCurrentCulture());
            templ = Utils.ReplaceSettingTokens(templ, StoreSettings.Current.Settings());
            templ = Utils.ReplaceUrlTokens(templ);
            return templ;
        }

        public static NBrightInfo GetProviderSettings(String ctrlkey)
        {
            var info = (NBrightInfo)Utils.GetCache("NBrightPayPalPaymentProvider" + PortalSettings.Current.PortalId.ToString(""));
            if (info == null)
            {
                var modCtrl = new NBrightBuyController();

                info = modCtrl.GetByGuidKey(PortalSettings.Current.PortalId, -1, "NBrightPayPalPAYMENT", ctrlkey);

                if (info == null)
                {
                    info = new NBrightInfo(true);
                    info.GUIDKey = ctrlkey;
                    info.TypeCode = "NBrightPayPalPAYMENT";
                    info.ModuleId = -1;
                    info.PortalId = PortalSettings.Current.PortalId;
                }

                Utils.SetCache("NBrightPayPalPaymentProvider" + PortalSettings.Current.PortalId.ToString(""), info);
            }

            return info;
        }

        public static String GetBankRemotePost(OrderData orderData)
        {
            // use this class to build up the post html
            var rPost = new RemotePost();

            // get the gateway settings which have been entered into the back office page (settings.html template)
            var settings = ProviderUtils.GetProviderSettings("NBrightPayPalpayment");

            // get the order data
            var payData = new PayData(orderData);

            rPost.Url = payData.PostUrl;

            rPost.Add("cmd", "_xclick");
            rPost.Add("item_number", payData.ItemId);
            rPost.Add("return", payData.ReturnUrl);
            rPost.Add("currency_code", payData.CurrencyCode);
            rPost.Add("cancel_return", payData.ReturnCancelUrl);
            rPost.Add("notify_url", payData.NotifyUrl);
            rPost.Add("custom", Utils.GetCurrentCulture());
            rPost.Add("business", payData.PayPalId);
            rPost.Add("item_name", orderData.PurchaseInfo.GetXmlProperty("genxml/ordernumber"));
            rPost.Add("amount", payData.Amount);
            rPost.Add("shipping", payData.ShippingAmount);
            rPost.Add("tax", payData.TaxAmount);
            rPost.Add("lc", Utils.GetCurrentCulture().Substring(3, 2));

            var extrafields = settings.GetXmlProperty("genxml/textbox/extrafields");
            var fields = extrafields.Split(',');
            foreach (var f in fields)
            {
                var ary = f.Split('=');
                if (ary.Count() == 2)
                {
                    var n = ary[0];
                    var v = ary[1];
                    var d = orderData.PurchaseInfo.GetXmlProperty(v);
                    rPost.Add(n, d);
                }
            }

            //Build the re-direct html 
            var rtnStr = rPost.GetPostHtml("/DesktopModules/NBright/NBrightPayPal/Themes/config/img/paypal.gif");
            if (settings.GetXmlPropertyBool("genxml/checkbox/debug.mode"))
            {
                File.WriteAllText(PortalSettings.Current.HomeDirectoryMapPath + "\\debug_NBrightPayPalpost.html", rtnStr);
            }
            return rtnStr;
        }

        public static bool VerifyPayment(PayPalIpnParameters ipn, string verifyURL)
        {
            try
            {
                bool isVerified = false;

                if (ipn.IsValid)
                {
                    System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

                    HttpWebRequest PPrequest = (HttpWebRequest)WebRequest.Create(verifyURL);
                    if ((PPrequest != null))
                    {
                        PPrequest.Method = "POST";
                        PPrequest.ContentLength = ipn.PostString.Length;
                        PPrequest.ContentType = "application/x-www-form-urlencoded";
                        StreamWriter writer = new StreamWriter(PPrequest.GetRequestStream());
                        writer.Write(ipn.PostString);
                        writer.Close();
                        HttpWebResponse response = (HttpWebResponse)PPrequest.GetResponse();
                        if ((response != null))
                        {
                            StreamReader reader = new StreamReader(response.GetResponseStream());
                            string responseString = reader.ReadToEnd();
                            reader.Close();
                            if (string.Compare(responseString, "VERIFIED", true) == 0)
                            {
                                isVerified = true;
                            }
                        }
                    }
                }
                return isVerified;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private string PayPalEncode(string value)
        {
            //a single accentuated/special character matches a single non acc/spec character:
            value = StringListReplace(value, "ŠŽŸÀÁÂÃÅÇÈÉÊËÌÍÎÏÐÑÒÓÔÕØÙÚÛÝÞØ", "SZYAAAAACEEEEIIIIDNOOOOOUUUYPO");
            value = StringListReplace(value, "šžÿàáâãåçèéêëìíîïðñòóôõøùúûýþµ", "szyaaaaaceeeeiiiidnooooouuuypu");

            //a single accentuated/special character matches a couple of non acc/spec character:
            value = value.Replace("Œ", "OE");
            value = value.Replace("Æ", "AE");
            value = value.Replace("œ", "oe");
            value = value.Replace("æ", "ae");

            return HttpUtility.UrlEncode(value);
        }

        private string StringListReplace(string value, string searchfor, string replacewith)
        {
            for (var x = 1; x <= searchfor.Length; x++)
            {
                value = value.Replace(searchfor.Substring(x - 1, 1), replacewith.Substring(x - 1, 1));
            }
            return value;
        }
    }
}
