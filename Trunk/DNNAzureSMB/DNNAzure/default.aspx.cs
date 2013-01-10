using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure;

namespace DNNAzure
{
    public partial class _default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            bool showDetails = false;
            try
            {
                showDetails = RoleEnvironment.IsEmulated
                              || Request.UserHostName.ToLower() == "localhost"
                              || Request.UserHostName.ToLower() == "admin.dnndev.me"
                              || bool.Parse(RoleEnvironment.GetConfigurationSettingValue("ShowDeploymentProgressDetails"));
                if (!IsPostBack && showDetails)
                {
                    grdLog.RowDataBound += GrdLogOnRowDataBound;
                    RefreshLog();
                }
                    
            }
            catch (Exception ex)
            {
                lblError.Text = "There was an error loading the page" + (showDetails? ex.Message:"");
            }
        }

        private void GrdLogOnRowDataBound(object sender, GridViewRowEventArgs args)
        {
            try
            {
                if (args.Row.RowType == DataControlRowType.DataRow)
                {
                    var dataItem = (Components.WadLogEntity) args.Row.DataItem;
                    switch (dataItem.Level)
                    {
                        case 2:
                            args.Row.Cells[0].CssClass = "Error";
                            break;
                        case 3:
                            args.Row.Cells[0].CssClass = "Warning";
                            break;
                    }
                }
            }
            catch
            {
                lblError.Text = "Databound error";
            }
        }

        private void RefreshLog()
        {
            var azureConnectionString =
                RoleEnvironment.GetConfigurationSettingValue(
                    "Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString");

            var account = CloudStorageAccount.Parse(azureConnectionString);
            var tableClient = account.CreateCloudTableClient();
            if (tableClient.ListTables().FirstOrDefault(x => x == "WADLogsTable") != null)
            {
                var dataCtx = tableClient.GetDataServiceContext();
                var results = dataCtx.CreateQuery<Components.WadLogEntity>("WADLogsTable").Where(
                   x => x.DeploymentId == RoleEnvironment.DeploymentId).ToList();
                grdLog.DataSource = results;
                grdLog.DataBind();
            }
                     
        }
    }
}