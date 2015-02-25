using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics.Eventing.Reader;
using System.Dynamic;
using System.Runtime.Remoting.Channels;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Util;
using DotNetNuke.Common;
using DotNetNuke.Entities.Portals;
using NBrightCore.common;
using Nevoweb.DNN.NBrightBuy.Components;

namespace Nevoweb.DNN.NBrightBuyPayPal
{

    public class PayData
    {

        public PayData(OrderData oInfo)
        {
            LoadSettings(oInfo);
        }

        public void LoadSettings(OrderData oInfo)
        {
            var settings = ProviderUtils.GetProviderSettings("NBrightPayPalpayment");
            var appliedtotal = oInfo.PurchaseInfo.GetXmlPropertyDouble("genxml/appliedtotal");
            var alreadypaid = oInfo.PurchaseInfo.GetXmlPropertyDouble("genxml/alreadypaid");

            ItemId = oInfo.PurchaseInfo.ItemID.ToString("");
            PostUrl = settings.GetXmlProperty("genxml/textbox/paymenturl");
            VerifyUrl = settings.GetXmlProperty("genxml/textbox/verifyurl");
            PayPalId = settings.GetXmlProperty("genxml/textbox/paypalid");
            CartName = "NBrightStore";

            CurrencyCode = oInfo.PurchaseInfo.GetXmlProperty("genxml/currencycode");
            if (CurrencyCode == "") CurrencyCode = settings.GetXmlProperty("genxml/textbox/currencycode");

            var param = new string[3];
            param[0] = "orderid=" + oInfo.PurchaseInfo.ItemID.ToString("");
            param[1] = "status=1";
            ReturnUrl = Globals.NavigateURL(StoreSettings.Current.PaymentTabId, "", param);
            param[0] = "orderid=" + oInfo.PurchaseInfo.ItemID.ToString("");
            param[1] = "status=0";
            ReturnCancelUrl = Globals.NavigateURL(StoreSettings.Current.PaymentTabId, "", param);
            NotifyUrl = Utils.ToAbsoluteUrl("/DesktopModules/NBright/NBrightPayPal/notify.ashx");
            MerchantLanguage = Utils.GetCurrentCulture();
            Amount = (appliedtotal - alreadypaid).ToString("0.00");
            Email = oInfo.PurchaseInfo.GetXmlProperty("genxml/billaddress/textbox/billaddress");
            if (!Utils.IsEmail(Email)) Email = oInfo.PurchaseInfo.GetXmlProperty("genxml/extrainfo/textbox/cartemailaddress");
            ShippingAmount = oInfo.PurchaseInfo.GetXmlPropertyDouble("genxml/shippingcost").ToString("0.00");
            TaxAmount = oInfo.PurchaseInfo.GetXmlPropertyDouble("genxml/taxcost").ToString("0.00");
        }

        public string ItemId { get; set; }
        public string PostUrl { get; set; }
        public string VerifyUrl { get; set; }
        public string PayPalId { get; set; }
        public string CartName { get; set; }
        public string CurrencyCode { get; set; }
        public string ReturnUrl { get; set; }
        public string ReturnCancelUrl { get; set; }
        public string NotifyUrl { get; set; }
        public string MerchantLanguage { get; set; }
        public string Amount { get; set; }
        public string Email { get; set; }
        public string ShippingAmount { get; set; }
        public string TaxAmount { get; set; }

        
        

    }

    public class PayPalIpnParameters 
    {

        public PayPalIpnParameters(NameValueCollection requestForm)
        {
            _postString = "cmd=_notify-validate";
            foreach (string paramName in requestForm)
            {
                _postString += string.Format("&{0}={1}", paramName, HttpContext.Current.Server.UrlEncode(requestForm[paramName]));
                switch (paramName)
                {
                    case "payment_status":
                        _payment_status = requestForm[paramName];
                        break;
                    case "item_number":
                        _item_number = Convert.ToInt32(requestForm[paramName]);
                        break;
                    case "custom":
                        _custom = requestForm[paramName];
                        break;
                }
            }
        }

        private string _postString = string.Empty;
        private string _payment_status = string.Empty;
        private string _txn_id = string.Empty;
        private string _receiver_email = string.Empty;
        private string _email = string.Empty;
        private string _custom = "";
        private int _item_number = -1;
        private decimal _mc_gross = -1;
        private decimal _shipping = -1;

        private decimal _tax = -1;
        public string PostString
        {
            get { return _postString; }
            set { _postString = value; }
        }

        public string payment_status
        {
            get { return _payment_status; }
            set { _payment_status = value; }
        }

        public string txn_id
        {
            get { return _txn_id; }
            set { _txn_id = value; }
        }

        public string receiver_email
        {
            get { return _receiver_email; }
            set { _receiver_email = value; }
        }

        public string email
        {
            get { return _email; }
            set { _email = value; }
        }

        public string custom
        {
            get { return _custom; }
            set { _custom = value; }
        }

        public int item_number
        {
            get { return _item_number; }
            set { _item_number = value; }
        }

        public decimal mc_gross
        {
            get { return _mc_gross; }
            set { _mc_gross = value; }
        }

        public decimal shipping
        {
            get { return _shipping; }
            set { _shipping = value; }
        }

        public decimal tax
        {
            get { return _tax; }
            set { _tax = value; }
        }

        public int CartID
        {
            get { return _item_number; }
        }

        public bool IsValid
        {
            get
            {
                if (_payment_status != "Completed" & _payment_status != "Pending")
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

    }


}
