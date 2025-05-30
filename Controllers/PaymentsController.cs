using GoPay;
using GoPay.Common;
using GoPay.Model.Payment;
using GoPay.Model.Payments;
using GoPayIntegration.Models;
using Microsoft.AspNetCore.Mvc;

namespace GoPayIntegration.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly GPConnector _connector;
        private readonly IConfiguration _configuration;

        public PaymentsController(IConfiguration configuration)
        {
            _configuration = configuration;
            var clientId = _configuration["GoPay:ClientId"] ?? throw new ArgumentNullException("GoPay:ClientId is missing");
            var clientSecret = _configuration["GoPay:ClientSecret"] ?? throw new ArgumentNullException("GoPay:ClientSecret is missing");
            var goId = _configuration["GoPay:GoId"] ?? throw new ArgumentNullException("GoPay:GoId is missing");
            var apiUrl = _configuration["GoPay:ApiUrl"] ?? "https://gw.sandbox.gopay.com/api";

            _connector = new GPConnector(apiUrl, clientId, clientSecret);
            _connector.GetAppToken(); // Authenticate with GoPay
        }

        [HttpPost("create")]
        public IActionResult CreatePayment([FromBody] PaymentRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var goIdString = _configuration["GoPay:GoId"] ?? throw new ArgumentNullException("GoPay:GoId is missing");
                var goId = long.Parse(goIdString);

                var payment = new BasePayment
                {
                    Currency = Enum.Parse<Currency>(request.Currency),
                    Lang = "ENG",
                    OrderNumber = request.OrderNumber,
                    Amount = (long)(request.Amount * 100), // Convert to cents
                    Target = new Target
                    {
                        GoId = goId,
                        Type = Target.TargetType.ACCOUNT
                    },
                    Callback = new Callback
                    {
                        NotificationUrl = _configuration["Callback:NotificationUrl"],
                        ReturnUrl = _configuration["Callback:ReturnUrl"]
                    },
                    Payer = new Payer
                    {
                        Contact = new PayerContact { Email = request.Email },
                        DefaultPaymentInstrument = PaymentInstrument.PAYMENT_CARD
                    }
                };

                var result = _connector.CreatePayment(payment);
                return Ok(new { PaymentId = result.Id, PaymentUrl = result.GwUrl });
            }
            catch (GPClientException ex)
            {
                return BadRequest(new { Errors = ex.Error.ErrorMessages });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("notify")]
        public IActionResult HandleNotification()
        {
            try
            {
                // Extract payment ID from query string
                var paymentIdString = Request.Query["id"].FirstOrDefault();
                if (string.IsNullOrEmpty(paymentIdString) || !long.TryParse(paymentIdString, out var paymentId))
                {
                    return BadRequest(new { Error = "Invalid or missing payment ID" });
                }

                // Check payment status using GoPay API
                var paymentStatus = _connector.PaymentStatus(paymentId);
                bool isPaid = paymentStatus.State.HasValue && paymentStatus.State.Value == Payment.SessionState.PAID;

                // TODO: Update your database or log the payment status
                return Ok(new { PaymentId = paymentId, IsPaid = isPaid, Status = paymentStatus.State.ToString() });
            }
            catch (GPClientException ex)
            {
                return BadRequest(new { Errors = ex.Error.ErrorMessages });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}