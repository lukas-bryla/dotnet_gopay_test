namespace GoPayIntegration.Models;

public class PaymentRequest
{
    public decimal Amount { get; set; } // Amount in whole units (e.g., 75.00)
    public string Currency { get; set; } // e.g., "CZK"
    public string OrderNumber { get; set; } // Unique order ID
    public string Email { get; set; } // Payer's email
}