using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace MiniProject_Karakara
{
    public partial class ManageProductPage : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Session["username"] == null || Session["role"].ToString() != "admin")
                {
                    Session.RemoveAll();
                    Response.Redirect("LoginPage.aspx");
                }
            }
        }

        protected void gvProducts_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            GridViewRow row = gvProducts.Rows[e.RowIndex];
            FileUpload fuEditImage = (FileUpload)row.FindControl("fuEditImage");
            Image imgEditPreview = (Image)row.FindControl("imgEditPreview");

            string fileName = "";

            if (fuEditImage != null && fuEditImage.HasFile)
            {
                fileName = Path.GetFileName(fuEditImage.FileName);
                string savePath = Server.MapPath("~/Images/" + fileName);
                fuEditImage.SaveAs(savePath);
            }
            else if (imgEditPreview != null && !string.IsNullOrEmpty(imgEditPreview.ImageUrl))
            {
                // Extract filename from ImageUrl (e.g., "~/Images/sofa.png")
                fileName = Path.GetFileName(imgEditPreview.ImageUrl);
            }

            // Always update the ProductImage parameter
            e.NewValues["ProductImage"] = fileName;
        }


    }
}