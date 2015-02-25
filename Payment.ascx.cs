
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using DotNetNuke.Common;
using DotNetNuke.Entities.Portals;
using NBrightCore.common;
using NBrightCore.render;
using NBrightDNN;

using Nevoweb.DNN.NBrightBuy.Base;
using Nevoweb.DNN.NBrightBuy.Components;

using DataProvider = DotNetNuke.Data.DataProvider;

namespace Nevoweb.DNN.NBrightBuyPayPal
{

    /// -----------------------------------------------------------------------------
    /// <summary>
    /// The ViewNBrightGen class displays the content
    /// </summary>
    /// -----------------------------------------------------------------------------
    public partial class NBrightPayPalPayment : NBrightBuyAdminBase
    {



        #region Event Handlers

        private String _ctrlkey = "";
        private NBrightInfo _info;

        override protected void OnInit(EventArgs e)
        {
            base.OnInit(e);

            try
            {
                _ctrlkey = "nbrightpaypalpayment";
                _info = ProviderUtils.GetProviderSettings(_ctrlkey);
                var rpDataHTempl = ProviderUtils.GetTemplateData("settings.html");
                rpDataH.ItemTemplate = NBrightBuyUtils.GetGenXmlTemplate(rpDataHTempl, StoreSettings.Current.Settings(), PortalSettings.HomeDirectory);
            }
            catch (Exception exc)
            {
                //display the error on the template (don;t want to log it here, prefer to deal with errors directly.)
                var l = new Literal();
                l.Text = exc.ToString();
                Controls.Add(l);
            }

        }

        protected override void OnLoad(EventArgs e)
        {
            try
            {
                base.OnLoad(e);

                if (Page.IsPostBack == false)
                {
                    PageLoad();
                }
            }
            catch (Exception exc) //Module failed to load
            {
                //display the error on the template (don;t want to log it here, prefer to deal with errors directly.)
                var l = new Literal();
                l.Text = exc.ToString();
                Controls.Add(l);
            }
        }

        private void PageLoad()
        {
            if (UserId > 0) // only logged in users can see data on this module.
            {
                // display header
                base.DoDetail(rpDataH, _info);

            }
        }

        #endregion

        #region  "Events "

        protected void CtrlItemCommand(object source, RepeaterCommandEventArgs e)
        {
            var cArg = e.CommandArgument.ToString();
            var param = new string[3];

            switch (e.CommandName.ToLower())
            {
                case "save":
                    Update();
                    param[0] = "ctrl=nbrightpaypalpayment";
                    Response.Redirect(Globals.NavigateURL(TabId, "", param), true);
                    break;
                case "cancel":
                    Response.Redirect(Globals.NavigateURL(TabId, "", param), true);
                    break;
            }

        }

        #endregion



        private void Update()
        {
            var modCtrl = new NBrightBuyController();
            var strXml = GenXmlFunctions.GetGenXml(rpDataH);
            _info.XMLData = strXml;
            _info.SetXmlProperty("genxml/debugmsg", "");
            modCtrl.Update(_info);

            var resxDic = GenXmlFunctions.GetGenXmlResx(rpDataH);
            var genTempl = (GenXmlTemplate)rpDataH.ItemTemplate;
            var resxfolders = genTempl.GetResxFolders();
            var resxUpdate = NBrightBuyUtils.UpdateResxFields(resxDic, resxfolders, StoreSettings.Current.EditLanguage, true);

            //remove current setting from cache for reload
            Utils.RemoveCache("NBrightPayPalPaymentProvider" + PortalSettings.Current.PortalId.ToString(""));

        }





    }

}
