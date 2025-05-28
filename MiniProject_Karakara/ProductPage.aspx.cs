using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace MiniProject_Karakara
{
    public partial class ProductPage : System.Web.UI.Page
    {
        // property to track selected product
        public int SelectedProductID
        {
            get { return (int)(ViewState["SelectedProductID"] ?? 0); }
            set { ViewState["SelectedProductID"] = value; }
        }

        // helper method to return css class
        public string GetProductCardCssClass(int productId)
        {
            return productId == SelectedProductID ? "product-card selected" : "product-card";
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                ddlCategory.DataBind();
                ddlCategory.Items.Insert(0, new ListItem("All Products", ""));

                if (Session["username"] == null)
                {
                    Response.Redirect("LoginPage.aspx");
                }
            }
        }

        protected void ddlCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectedProductID = 0;
        }

        protected void btnSelect_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            int productId = Convert.ToInt32(btn.CommandArgument);
            SelectedProductID = productId;
            lvProducts.DataBind();

        }

        protected void btnAddToCart_Click(object sender, EventArgs e)
        {
            lblMessage.Text = "";

            // validate selected product
            if (SelectedProductID == 0)
            {
                lblMessage.Text = "Please select a product first.";
                lblMessage.ForeColor = System.Drawing.Color.Red;
                return;
            }

            // validate quantity input
            int quantity;
            if (!int.TryParse(txtQuantity.Text, out quantity) || quantity <= 0)
            {
                lblMessage.Text = "Please enter a valid quantity.";
                lblMessage.ForeColor = System.Drawing.Color.Red;
                return;
            }

            string username = Session["username"].ToString();
            int productId = SelectedProductID;
            string connDB = ConfigurationManager.ConnectionStrings["ConnKarakara"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connDB))
            {
                conn.Open();

                // get user id from username
                int userId = 0;
                using (SqlCommand cmd = new SqlCommand("SELECT Id FROM Users WHERE username = @username", conn))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    object result = cmd.ExecuteScalar();
                    if (result == null)
                    {
                        lblMessage.Text = "User not found.";
                        lblMessage.ForeColor = System.Drawing.Color.Red;
                        return;
                    }
                    userId = Convert.ToInt32(result);
                }

                // get product name
                string productName = string.Empty;
                using (SqlCommand cmd = new SqlCommand("SELECT ProductName FROM Products WHERE ProductID = @productId", conn))
                {
                    cmd.Parameters.AddWithValue("@productId", productId);
                    object result = cmd.ExecuteScalar();
                    if (result == null)
                    {
                        lblMessage.Text = "Product not found.";
                        lblMessage.ForeColor = System.Drawing.Color.Red;
                        return;
                    }
                    productName = result.ToString();
                }

                // insert to user cart
                using (SqlCommand cmd = new SqlCommand("INSERT INTO UserCart (UserId, ProductId, quantity) VALUES (@userId, @productId, @quantity)", conn))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@productId", productId);
                    cmd.Parameters.AddWithValue("@quantity", quantity);
                    cmd.ExecuteNonQuery();
                }

                lblMessage.ForeColor = System.Drawing.Color.Green;
                lblMessage.Text = "x" + quantity + " " + productName + " added to cart successfully!";
                SelectedProductID = 0;
                txtQuantity.Text = "";
                lvProducts.DataBind();
            }
        }
    }
}