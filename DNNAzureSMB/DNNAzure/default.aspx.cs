using System;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace DNNAzure
{
    public partial class _default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (RoleEnvironment.IsEmulated)
            {
                if (!string.IsNullOrEmpty(WebRole.WebSiteUrl))
                {
                    Response.Redirect(WebRole.WebSiteUrl);
                }
            }
        }
    }
}