using EntityProject.repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Retailbanking.BL.IServices;
using Retailbanking.Common.CustomObj;
using System.Threading.Tasks;
using PaymentNotificationOutsideOfFlutterwaveEntity = EntityProject.entities.PaymentNotificationOutsideOfFlutterwave;

namespace assetmanagement.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IGeneric _genServ;
        private readonly ILogger<IGenericAssetCapitalInsuranceCustomerService> _logger;
        private readonly IFlutterPaymentLink _flutterPaymentLink;
        private readonly IPaymentOutsideOfFlutterwave _paymentOutsideOfFlutterwave;

        public PaymentController(IPaymentOutsideOfFlutterwave paymentOutsideOfFlutterwave,IGeneric genServ, ILogger<IGenericAssetCapitalInsuranceCustomerService> logger, IFlutterPaymentLink flutterPaymentLink)
        {
            _genServ = genServ;
            _logger = logger;
            _flutterPaymentLink = flutterPaymentLink;
            _paymentOutsideOfFlutterwave=paymentOutsideOfFlutterwave;
        }


        /// <summary>
        ///Description: The endpoint to get payment  
        /// </summary>
        [HttpPost("GetPaymentLink/{AppType}/{Session}")]
        public async Task<GenericResponse2> GetPaymentLink(string AppType,[FromHeader] string AppKey, PaymentLinkRequestDto paymentLinkRequestDto,string Session)
        {
            _logger.LogInformation("paymentLinkRequestDto " +(JsonConvert.SerializeObject(paymentLinkRequestDto)));
            return await _flutterPaymentLink.GetPaymentLink(AppKey,AppType,paymentLinkRequestDto,Session); 
           //return new GenericResponse2() { Message = "active session.", Response = EnumResponse.Successful, Success = true, data = new { IsActive = true } };
        }

        /// <summary>
        ///Description: The endpoint allows to complete payment
        ///pass wallet or nil for PaymentChannelOptionForsubscription
        /// </summary>
        [HttpPost("GetPaymentResponseAfterTransaction")]
        public async Task<GenericResponse2> GetPaymentResponseAfterTransaction([FromHeader] string AppKey,GetPaymentResponseAfterTransaction getPaymentResponseAfterTransaction)
        {
            _logger.LogInformation("PaymentReference "+ JsonConvert.SerializeObject(getPaymentResponseAfterTransaction));
            return await _flutterPaymentLink.GetPaymentResponseAfterTransaction(AppKey,getPaymentResponseAfterTransaction);
        }

        /// <summary>
        ///Description: The endpoint to fund wallet after payment
        /// </summary>
        [HttpPost("FundWalletAfterTransaction")]
        public async Task<GenericResponse2> FundWalletAfterTransaction([FromHeader] string AppKey, FundWalletAfterTransaction fundWalletAfterTransaction)
        {
            _logger.LogInformation("FundWalletAfterTransaction " + JsonConvert.SerializeObject(fundWalletAfterTransaction));
            return await _flutterPaymentLink.FundWalletAfterTransaction(AppKey, fundWalletAfterTransaction);
        }

        /// <summary>
        ///Description: notification payment for customers that pay outside of flutterwave
        /// </summary>
        [HttpPost("SendExternalPaymentNotification")]
        public async Task<GenericResponse2> SendExternalPaymentNotification(PaymentNotificationOutsideOfFlutterwave paymentNotificationOutsideOfFlutterwave)
        {
            _logger.LogInformation("SendExternalPaymentNotification " + JsonConvert.SerializeObject(paymentNotificationOutsideOfFlutterwave));
           PaymentNotificationOutsideOfFlutterwaveEntity paymentNotificationOutsideOfFlutterwaveEntity = _paymentOutsideOfFlutterwave.GetIPaymentOutsideOfFlutterwaveByUserNameAndUserType(paymentNotificationOutsideOfFlutterwave.UserName,
                paymentNotificationOutsideOfFlutterwave.UserType,
                paymentNotificationOutsideOfFlutterwave.PaymentReference);
            if (paymentNotificationOutsideOfFlutterwaveEntity!=null)
            {
                return new GenericResponse2() {Response=EnumResponse.PaymentSubmittedAlready,Success=false,Message="Payment submitted already"};
            }
            paymentNotificationOutsideOfFlutterwaveEntity = new PaymentNotificationOutsideOfFlutterwaveEntity();
            paymentNotificationOutsideOfFlutterwaveEntity.Amount = paymentNotificationOutsideOfFlutterwave.Amount;
            paymentNotificationOutsideOfFlutterwaveEntity.AccountNumber = paymentNotificationOutsideOfFlutterwave.AccountNumber;
            paymentNotificationOutsideOfFlutterwaveEntity.BankName = paymentNotificationOutsideOfFlutterwave.BankName;
            paymentNotificationOutsideOfFlutterwaveEntity.PaymentReference = paymentNotificationOutsideOfFlutterwave.PaymentReference;
            paymentNotificationOutsideOfFlutterwaveEntity.UserName = paymentNotificationOutsideOfFlutterwave.UserName;
            paymentNotificationOutsideOfFlutterwaveEntity.UserType = paymentNotificationOutsideOfFlutterwave.UserType;
            _paymentOutsideOfFlutterwave.AddIPaymentOutsideOfFlutterwave(paymentNotificationOutsideOfFlutterwaveEntity);    
            return await _flutterPaymentLink.SendExternalPaymentNotification(paymentNotificationOutsideOfFlutterwave.Session,
                paymentNotificationOutsideOfFlutterwave.UserName,
                paymentNotificationOutsideOfFlutterwave.UserType,
                paymentNotificationOutsideOfFlutterwave.Amount,
                paymentNotificationOutsideOfFlutterwave.PaymentReference,
                paymentNotificationOutsideOfFlutterwave.BankName,
                paymentNotificationOutsideOfFlutterwave.AccountNumber);
        }
    }
}









