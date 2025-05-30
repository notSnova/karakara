using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace MiniProject_Karakara
{
    public partial class CartPage : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Session["username"] == null)
                {
                    Response.Redirect("LoginPage.aspx");
                }

                // call the method to display cart summary
                CartSummary();
            }
        }

        protected void gvCart_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            // call the method for deletion of items
            string productName = gvCart.DataKeys[e.RowIndex].Values["ProductName"].ToString();
            string quantity = gvCart.DataKeys[e.RowIndex].Values["Quantity"].ToString();
            Session["CartDeletedItem"] = $"x{quantity} <strong>{productName}</strong> has been removed from your cart.";
        }

        protected void gvCart_Deleted(object sender, GridViewDeletedEventArgs e)
        {
            // call the method after deletion of items
            if (Session["CartDeletedItem"] != null)
            {
                string cartDeletedItem = Session["CartDeletedItem"].ToString();
                lblMessage.Text = cartDeletedItem;
                lblMessage.ForeColor = System.Drawing.Color.Green;
                Session.Remove("CartDeletedItem"); 
            }
            else
            {
                lblMessage.Text = "Item removed from your cart.";
            }
            CartSummary();
        }

        private void CartSummary()
        {
            DataView dv = (DataView)SqlDataSource2.Select(DataSourceSelectArguments.Empty);
            
            if (dv != null && dv.Count > 0 && dv[0]["TotalBeforeTax"] != DBNull.Value)
            {
                decimal total = Convert.ToDecimal(dv[0]["TotalBeforeTax"]);

                if(total > 0)
                {
                    decimal tax = total * 0.06m;
                    decimal amountToPay = total + tax;

                    lblTotal.Text = "RM " + total.ToString("N2");
                    lblTax.Text = "RM " + tax.ToString("N2");
                    lblAmountToPay.Text = "RM " + amountToPay.ToString("N2");

                    summaryHide.Visible = true;
                    btnConfirmOrder.Visible = true;
                    return;
                }
            }

            //if total is 0, hide summary
            summaryHide.Visible = false;
            btnConfirmOrder.Visible = false;
        }

        protected void btnConfirmOrder_Click(object sender, EventArgs e)
        {
            string username = Session["username"]?.ToString();
            string orderNumber = GenerateOrderNumber();
            DateTime orderDate = DateTime.Now;

            // get cart items
            string connectionString = ConfigurationManager.ConnectionStrings["ConnKarakara"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                
                // get user's cart from procedure
                SqlCommand getCartCmd = new SqlCommand("GetUserCart", conn);
                getCartCmd.CommandType = CommandType.StoredProcedure;
                getCartCmd.Parameters.AddWithValue("@Username", username);

                SqlDataAdapter da = new SqlDataAdapter(getCartCmd);
                DataTable cartItems = new DataTable();
                da.Fill(cartItems);

                if (cartItems.Rows.Count == 0 )
                {
                    return;
                }

                // get user id
                SqlCommand getUserIDcmd = new SqlCommand("SELECT Id FROM Users WHERE username = @Username", conn);
                getUserIDcmd.Parameters.AddWithValue("@Username", username);
                object result = getUserIDcmd.ExecuteScalar();
                int userId = Convert.ToInt32(result);

                // insert order into Orders table
                SqlCommand insertOrderCmd = new SqlCommand("INSERT INTO Orders (OrderNumber, UserID, OrderDate, TotalAmount, Tax, AmountToPay) OUTPUT INSERTED.OrderID VALUES (@OrderNumber, @UserID, @OrderDate, @Total, @Tax, @AmountToPay)", conn);

                decimal total = decimal.Parse(lblTotal.Text.Replace("RM ", "").Trim());
                decimal tax = decimal.Parse(lblTax.Text.Replace("RM ", "").Trim());
                decimal amountToPay = decimal.Parse(lblAmountToPay.Text.Replace("RM ", "").Trim());

                insertOrderCmd.Parameters.AddWithValue("@OrderNumber", orderNumber);
                insertOrderCmd.Parameters.AddWithValue("@UserID", userId);
                insertOrderCmd.Parameters.AddWithValue("@OrderDate", orderDate);
                insertOrderCmd.Parameters.AddWithValue("@Total", total);
                insertOrderCmd.Parameters.AddWithValue("@Tax", tax);
                insertOrderCmd.Parameters.AddWithValue("@AmountToPay", amountToPay);

                int orderId = (int)insertOrderCmd.ExecuteScalar();

                // insert each cart item into OrderItems table
                foreach (DataRow row in cartItems.Rows)
                {
                    SqlCommand insertOrderItemCmd = new SqlCommand("INSERT INTO OrderItems (OrderID, ProductID, Quantity, Price) VALUES (@OrderID, @ProductID, @Quantity, @Price)", conn);
                    insertOrderItemCmd.Parameters.AddWithValue("@OrderID", orderId);
                    insertOrderItemCmd.Parameters.AddWithValue("@ProductID", row["ProductID"]);
                    insertOrderItemCmd.Parameters.AddWithValue("@Quantity", row["Quantity"]);
                    insertOrderItemCmd.Parameters.AddWithValue("@Price", row["Price"]);

                    insertOrderItemCmd.ExecuteNonQuery();
                }

                SqlCommand clearCartCmd = new SqlCommand("DELETE FROM UserCart WHERE UserID = @UserId", conn);
                clearCartCmd.Parameters.AddWithValue("@UserId", userId);
                clearCartCmd.ExecuteNonQuery();

                conn.Close();
                Session["order-added"] = "Your order " + orderNumber + " has been confirmed!";
                Response.Redirect("OrderHistoryPage.aspx");
            }
        }

        private string GenerateOrderNumber()
        {
            return "ORD" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
        }
    }
}