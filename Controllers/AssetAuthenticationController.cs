using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Retailbanking.BL.IServices;
using Retailbanking.Common.CustomObj;
using System.Data.SqlTypes;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using EntityProject.repositories;
using EntityProject.repositoriesimpl;
using Retailbanking.Common.DbObj;

namespace assetmanagement.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class AssetAuthenticationController : ControllerBase
    {
        private readonly IGeneric _genServ;
        private readonly ILogger<IGenericAssetCapitalInsuranceCustomerService> _logger;
        private readonly IGenericAssetCapitalInsuranceCustomerService _customerServ;
        private readonly IRegistrationRepository registrationRepository;
        private readonly IUserRepository userRepository;
        private readonly IOtpSessionRepository sessionRepository;
        private readonly IUserCredentialsRepository userCredentialsRepository;
        private readonly IUserSessionRepository userSessionRepository;
        private readonly IRegistrationSessionRepository registrationSessionRepostory;
        private readonly ICustomerDeviceRepository customerDeviceRepository;
        private readonly ICustomerDataNotFromBvnRepository customerDataFromBvnRepository;

        public AssetAuthenticationController(IGeneric genServ, ILogger<IGenericAssetCapitalInsuranceCustomerService> logger, IGenericAssetCapitalInsuranceCustomerService customerServ, IRegistrationRepository registrationRepository, IUserRepository userRepository, IOtpSessionRepository sessionRepository, IUserCredentialsRepository userCredentialsRepository, IUserSessionRepository userSessionRepository, IRegistrationSessionRepository registrationSessionRepostory, ICustomerDeviceRepository customerDeviceRepository, ICustomerDataNotFromBvnRepository customerDataFromBvnRepository)
        {
            _genServ = genServ;
            _logger = logger;
            _customerServ = customerServ;
            this.registrationRepository = registrationRepository;
            this.userRepository = userRepository;
            this.sessionRepository = sessionRepository;
            this.userCredentialsRepository = userCredentialsRepository;
            this.userSessionRepository = userSessionRepository;
            this.registrationSessionRepostory = registrationSessionRepostory;
            this.customerDeviceRepository = customerDeviceRepository;
            this.customerDataFromBvnRepository = customerDataFromBvnRepository;
        }

        [HttpPost("ClearRegistrationByBvn/{Bvn}/{UserType}")]
        public async Task<GenericResponse> ClearRegistrationByBvn(string Bvn, string UserType)
        {
            return await _customerServ.ClearRegistrationByBvn(Bvn,UserType);
          //  AssetCapitalInsuranceUsers usr =await _genServ.GetAssetCapitalInsuranceUserbyBvn();
        }

        /*
        public IActionResult CheckStatus()
        {
            // Retrieve user information from session
            var username = HttpContext.Session.GetString("Username");
            var loginTime = HttpContext.Session.GetString("LoginTime");
            if (string.IsNullOrEmpty(username))
            {
                return Ok("No active session.");
            }
            return Ok($"User {username} logged in at {loginTime}.");
        }
        */

        [HttpGet("AppUserSessionStatus/{UserName}")]
        public async Task<GenericResponse2> CheckSessionStatus(string UserName)
        {
            var username = HttpContext.Session.GetString(UserName+"_prime");
            
            if (string.IsNullOrEmpty(username))
            {
                return new GenericResponse2() { Message = "No active session.", Response = EnumResponse.Successful, Success = true,data=new { IsActive = false } };
            }
            return new GenericResponse2() { Message = "active session.", Response = EnumResponse.Successful, Success = true, data = new { IsActive = true } };
        }

        [HttpPost("LoginUser/{UserType}")]
        public async Task<LoginResponse> LoginUser(LoginRequest Request,string UserType)
        {
             HttpContext.Session.SetString(Request.Username+"_prime",Request.Username);
            HttpContext.Session.SetString("LoginTime", DateTime.Now.ToString());
            var resp = await _customerServ.LoginUser("", Request,UserType);
            Request.Password = "";
            _genServ.LogRequestResponse("LoginUser", JsonConvert.SerializeObject(Request), JsonConvert.SerializeObject(resp));
            string host = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host + HttpContext.Request.PathBase;
            string picpath = host + "/" + "api/FileService/BrowserView/" + resp.ProfilePic;
            _genServ.LogRequestResponse("picpath", picpath, "");
            resp.ProfilePic = picpath;
            return resp;
        }

        [HttpPost("ValidateOtp/{UserType}")]
        public async Task<GenericResponse> ValidateOtp(AssetCapitalInsuranceValidateOtpRetrival Request,string UserType)
        {
            var resp = await _customerServ.ValidateOtpForOtherPurposes("", Request,UserType);
            _genServ.LogRequestResponse("ValidateOtp", JsonConvert.SerializeObject(Request), JsonConvert.SerializeObject(resp));
            return resp;
        }

        [HttpPost("MailAfterLoginAndSuccessful/{UserType}")]
        public async Task<GenericResponse2> MailAfterLoginAndSuccessful(Mailer mailer,string UserType)
        {
            var resp = await _customerServ.MailAfterLoginAndSuccessfulAccountFetch(mailer.Username, mailer.Session, mailer.ChannelId, mailer.DeviceName,UserType);
            _genServ.LogRequestResponse("MailAfterLoginAndSuccessfulAccountFetch", mailer.Username, JsonConvert.SerializeObject(resp));
            return resp;
        }

        [HttpPost("LoginUserFingerPrint/{UserType}")]
        public async Task<LoginResponse> LoginUserFingerPrint(LoginRequestFinger Request,string UserType)
        {
            var request = new LoginRequest()
            {
                Device = Request.Device,
                GPS = Request.GPS,
                Username = Request.Username,
                ChannelId = Request.ChannelId,
            };
            var resp = await _customerServ.LoginUser("", request,UserType, true);
            string host = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host + HttpContext.Request.PathBase;
            //  string picpath = host + "/" + resp.ProfilePic;
            string picpath = host + "/" + "api/FileService/BrowserView/" + resp.ProfilePic;
            _genServ.LogRequestResponse("picpath", picpath, "");
            resp.ProfilePic = picpath;
            resp.ProfilePic = picpath;
            _genServ.LogRequestResponse("LoginUserFingerPrint", JsonConvert.SerializeObject(Request), JsonConvert.SerializeObject(resp));
            return resp;
        }

        /// <summary>
        /// to reset password or username
        /// </summary>
        [HttpPost("StartRetrival/{UserType}")]
        public async Task<GenericResponse> StartRetrival(AssetCapitalInsuranceResetObj Request,string UserType)
        {
            _logger.LogInformation("AssetCapitalInsuranceResetObj " + JsonConvert.SerializeObject(Request));
            var resp = await _customerServ.StartRetrival("",Request,UserType);
            _genServ.LogRequestResponse("StartRetrival", JsonConvert.SerializeObject(Request), JsonConvert.SerializeObject(resp));
            return resp;
        }

        [HttpPost("ResetPassword/{UserType}")]
        public async Task<GenericResponse> ResetPassword(AssetCapitalInsuranceResetPassword Request,string UserType)
        {
            var resp = await _customerServ.ResetPassword("", Request,UserType);
            _genServ.LogRequestResponse("ResetPassword", JsonConvert.SerializeObject(Request), JsonConvert.SerializeObject(resp));
            return resp;
        }
        
        [HttpPost("ValidateOtherDeviceForOnBoarding/{UserType}")]
        public async Task<GenericResponse> ValidateOtherDeviceForOnBoarding(AssetCapitalInsurancePhoneAndAccount Request,string UserType)
        {
            var resp = await _customerServ.ValidateOtherDeviceForOnBoarding("", Request,UserType);
            return resp;
        }

        [HttpPost("ValidateOtpToOnBoardOtherDevices/{UserType}")]
        public async Task<GenericResponse> ValidateOtpToOnBoardOtherDevices(DeviceOtpValidator Request,string UserType)
        {
            var resp = await _customerServ.ValidateOtpToOnBoardOtherDevices("", Request, UserType);
            return resp;
        }

        [HttpPost("ValidatePin/{UserType}")]
        public async Task<GenericResponse> ValidatePin(PinValidator pinValidator,string UserType)
        {
            GenericResponse resp = await _customerServ.ValidatePin(pinValidator,UserType);
            return resp;
        }

        /// <summary>
        /// for pin,set type=pin
        /// </summary>
        [HttpGet("SendTypeOtp/{Username}/{UserType}")]
        public async Task<ValidateOtpResponse> SendTypeOtp(string Username,string UserType, [FromQuery] string TypeOtp)
        {
            if (string.IsNullOrEmpty(TypeOtp))
            {
                return new ValidateOtpResponse()
                {
                    Response = EnumResponse.NotSuccessful,
                    Message = "TypeOtp cannot be null or empty"
                };
            }
            var resp = await _customerServ.SendTypeOtp(OtpType.Registration, Username, UserType, TypeOtp);
            return resp;
        }

        /// <summary>
        /// Validate Nin for Existing user
        /// </summary>
        [HttpGet("ValidateAndCompareNinBvn/{Session}/{Username}/{ChannelId}/{Nin}/{UserType}")]
        public async Task<GenericResponse2> CompareNinAndBvnForValidation([FromHeader] string ClientKey, string Session, string Username, int ChannelId, string Nin,string UserType)
        {
            _genServ.LogRequestResponse("nin...", null, "calling nin service");
            var resp = await _customerServ.CompareNinAndBvnForValidation(ClientKey, Session, Username, ChannelId, Nin, UserType);
            Console.WriteLine("CompareNinAndBvnForValidation ...." + resp.ToString());
            _genServ.LogRequestResponse("nin ...", null, JsonConvert.SerializeObject(resp));
            return resp;
        }

    }
}








































