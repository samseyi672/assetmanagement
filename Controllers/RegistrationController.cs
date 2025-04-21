using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Retailbanking.Common.CustomObj;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;
using Retailbanking.BL.IServices;
using EntityProject.DBContext;
using Retailbanking.Common.DbObj;
using System.Threading;
using EntityProject.entities;
using UsersEntity = EntityProject.entities.Users;
using RegistrationEntity = EntityProject.entities.Registration;
using UserSessionEntity = EntityProject.entities.UserSession;
using CustomerDataNotFromBvnEntity = EntityProject.entities.CustomerDataNotFromBvn;
using EntityProject.repositoriesimpl;
using EntityProject.repositories;

namespace assetmanagement.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class RegistrationController : ControllerBase
    {

        private readonly IGeneric _genServ;
        private readonly IGenericAssetCapitalInsuranceCustomerService _customerServ;
        private readonly ILogger<IGenericAssetCapitalInsuranceCustomerService> _logger;
        private readonly IRegistrationRepository _registrationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IOtpSessionRepository _sessionRepository;
        private readonly IUserCredentialsRepository _userCredentialsRepository;
        private readonly IUserSessionRepository _userSessionRepository;
        private readonly IRegistrationSessionRepository _registrationSessionRepostory;
        private readonly ICustomerDeviceRepository _customerDeviceRepository;
        private readonly ICustomerDataNotFromBvnRepository _customerDataFromBvnRepository;
        private readonly IMobileDeviceRepository _mobileDeviceRepository;

        public RegistrationController(IGeneric genServ, IGenericAssetCapitalInsuranceCustomerService customerServ, ILogger<IGenericAssetCapitalInsuranceCustomerService> logger, IRegistrationRepository registrationRepository, IUserRepository userRepository, IOtpSessionRepository sessionRepository, IUserCredentialsRepository userCredentialsRepository, IUserSessionRepository userSessionRepository, IRegistrationSessionRepository registrationSessionRepostory, ICustomerDeviceRepository customerDeviceRepository, ICustomerDataNotFromBvnRepository customerDataFromBvnRepository, IMobileDeviceRepository mobileDeviceRepository)
        {
            _genServ = genServ;
            _customerServ = customerServ;
            _logger = logger;
            _registrationRepository = registrationRepository;
            _userRepository = userRepository;
            _sessionRepository = sessionRepository;
            _userCredentialsRepository = userCredentialsRepository;
            _userSessionRepository = userSessionRepository;
            _registrationSessionRepostory = registrationSessionRepostory;
            _customerDeviceRepository = customerDeviceRepository;
            _customerDataFromBvnRepository = customerDataFromBvnRepository;
            _mobileDeviceRepository = mobileDeviceRepository;
        }

        [HttpGet("ValidateUsername/{username}/{UserType}")]
        public async Task<GenericResponse> ValidateUsername(string username,string UserType) => await _customerServ.ValidateUsername("", username, UserType);


        [HttpPost("CreateUsername/{UserType}")]
        public async Task<GenericResponse> CreateUsername(SetRegristationCredential Request,string UserType)
        {
            var resp = await _customerServ.CreateUsername("", Request, UserType);
            _genServ.LogRequestResponse("CreateUsername", JsonConvert.SerializeObject(Request), JsonConvert.SerializeObject(resp));
            if (resp.Success)
            {
                UsersEntity users = _userRepository.GetUserByUserName(Request.SecretValue);
                if (users!=null)
                {
                    RegistrationEntity registrationEntity = _registrationRepository.GetRegistrationByUserName(Request.SecretValue);
                    if (registrationEntity!=null)
                    {
                        users.ClientUniqueRef = registrationEntity.client_unique_ref;
                        _userRepository.AddUser(users);
                    }
                }
            } 
            return resp;
        }

        [HttpPost("CreatePassword/{UserType}")]
        public async Task<GenericResponse> CreatePassword(SavePasswordRequest Request,string UserType)
        {
            var resp = await _customerServ.CreatePassword("", Request, UserType);
            _genServ.LogRequestResponse("CreatePassword", JsonConvert.SerializeObject(Request), JsonConvert.SerializeObject(resp));
            return resp;
        }

        [HttpPost("CreateTransPin")]
        public async Task<GenericResponse> CreateTransPin(SetRegristationCredential Request,string UserType)
        {
            Console.WriteLine("enter here");
            var resp = await _customerServ.CreateTransPin("", Request, UserType);
            _genServ.LogRequestResponse("CreateTransPin", JsonConvert.SerializeObject(Request), JsonConvert.SerializeObject(resp));
            return resp;
        }


        [HttpPost("StartRegistration")]
        public async Task<RegistrationResponse> RegistrationWithAccount(AssetCapitalInsuranceRegistrationRequest Request)
        {
            _logger.LogInformation("AssetCapitalInsuranceRegistrationRequest "+JsonConvert.SerializeObject(Request));
            var resp = await _customerServ.StartRegistration(Request);
            //checking 
            CustomerDataNotFromBvnEntity customerDataNotFromBvnEntity = _customerDataFromBvnRepository.GetUserByCustomerDataNotFromBvnPhoneNumber(resp?.PhoneNumber);
            if(customerDataNotFromBvnEntity==null)
            {
                CustomerDataNotFromBvnEntity cust1 = new CustomerDataNotFromBvnEntity();
                cust1.PhoneNumber = resp?.PhoneNumber;
                cust1.UserType = Request.UserType;
                cust1.Email = resp?.Email;
                cust1.ChannelId=Request.ChannelId;
                cust1.CreatedOn=DateTime.Now;
                cust1.PhoneNumberNotFromBvn=Request.PhoneNumber;
                _customerDataFromBvnRepository.AddCustomerDataNotFromBvn(cust1);
            }
            resp.Email = _genServ.MaskEmail(resp?.Email);
            _genServ.LogRequestResponse("StartRegistration", JsonConvert.SerializeObject(Request), JsonConvert.SerializeObject(resp));
            return resp;
        }


        /// <summary>
        ///  Description: This is to open or onboard on simplex for Cash Account and others
        ///  At this stage if successful , then the profile is fully created.
        ///  Integration with simplex is done at this stage
        ///  Please chech the hasSimplexAccount and ClientUniqueId for Existing Customer
        /// </summary>
        [HttpPost("OpenAccount/{UserType}/{RegId}")]
        public async Task<GenericResponse2> OpenAccount(AssetCapitalInsuranceGenericRegRequest Request, string UserType, int RegId)
        {
            _logger.LogInformation($"enters {UserType}");
            var resp2 = await _customerServ.GetDataAtRegistrationWithReference(RegId, UserType, Request.Session, Request.RequestReference, Request.ChannelId);
            if (resp2 == null || resp2.Success == false)
            {
                return new GenericResponse2() { Success = false, Response = EnumResponse.RestartRegistration };
            }
            var assetCapitalInsuranceRegistration = (AssetCapitalInsuranceRegistration)resp2.data;
            var resp3 = await _customerServ.GetRegistrationDataBeforeOpenAccount(assetCapitalInsuranceRegistration.bvn, UserType, Request.Session);
            BvnResponse bvnResponse = (BvnResponse)resp3.data;
            assetCapitalInsuranceRegistration.birth_date = bvnResponse.dateOfBirth;
            GenericRegRequest genericRegRequest = new GenericRegRequest();
            genericRegRequest.ChannelId = Request.ChannelId;
            genericRegRequest.RequestReference = Request.RequestReference;
            genericRegRequest.Session = Request.Session;
            if (!Request.hassimplexAccount && Request.ClientUniqueIdIfExists == 0)
            {
                var resp = await _customerServ.OpenAccount(UserType, genericRegRequest, assetCapitalInsuranceRegistration);  // call simplex here for onboarding
                _logger.LogInformation("resp " + JsonConvert.SerializeObject(resp));
                if (resp.Success)
                {
                    SimplexCustomerRegistrationResponse simplexCustomerRegistrationResponse = JsonConvert.DeserializeObject<SimplexCustomerRegistrationResponse>((string)(resp?.data));
                    _logger.LogInformation("simplexCustomerRegistrationResponse " + JsonConvert.SerializeObject(simplexCustomerRegistrationResponse));
                    //update registration entity
                    RegistrationEntity registration = _registrationRepository.GetRegistrationByBvnAndUserType(assetCapitalInsuranceRegistration.bvn, UserType);
                        registration.client_unique_ref = simplexCustomerRegistrationResponse.data.client_unique_ref;
                        _registrationRepository.AddRegistration(registration);
                    return resp;
                }

            }
            if (Request.hassimplexAccount && Request.ClientUniqueIdIfExists != 0)
            {
                var resp4 = new GenericResponse2() { Response = EnumResponse.Successful, Success = true };
                //update registration entity
                RegistrationEntity registration = _registrationRepository.GetRegistrationByBvnAndUserType(assetCapitalInsuranceRegistration.bvn,UserType);
                registration.client_unique_ref = (int)Request.ClientUniqueIdIfExists;
                _registrationRepository.AddRegistration(registration);
                return resp4;
            }
            //resp.Response = EnumResponse.NotSuccessful;
            var resp5 = new GenericResponse2() { Success = false,Response=EnumResponse.NotDataFound };
            return resp5;
        }

        /// <summary>
        ///  Description: GetRegistrationDataBeforeOpenAccount
        ///  
        /// </summary>
        [HttpGet("GetRegistrationDataBeforeOpenAccount/{Bvn}/{UserType}/{Session}")]
        public async Task<GenericResponse2> GetRegistrationDataBeforeOpenAccount(string Bvn, string UserType,string Session)
        {
            _logger.LogInformation($"enters {UserType}");
            var resp = await _customerServ.GetRegistrationDataBeforeOpenAccount(Bvn,UserType,Session);
            _genServ.LogRequestResponse("GetRegistrationDataBeforeOpenAccount",Bvn,"");
             var bvndata = (BvnResponse)resp.data;
            return resp;
           // return new GenericResponse2() { Success=true,Response=EnumResponse.Successful,data=bvndata };
        }

        [HttpPost("ValidateDob/{UserType}")]
        public async Task<GenericResponse> ValidateDob(SetRegristationCredential Request,string UserType)
        {
            var resp = await _customerServ.ValidateDob("", Request,UserType);
            _genServ.LogRequestResponse("ValidateDob", JsonConvert.SerializeObject(Request), JsonConvert.SerializeObject(resp));
            return resp;
        }

        [HttpPost("ValidateOtp/{UserType}")]
        public async Task<ValidateOtpResponse> ValidateOtp(AssetCapitalInsuranceValidateOtp Request,string UserType)
        {
            Console.WriteLine("Enters ValidateOtp");
            var resp = await _customerServ.ValidateOtp("", Request,UserType);
            _genServ.LogRequestResponse("ValidateOtp", JsonConvert.SerializeObject(Request), JsonConvert.SerializeObject(resp));
            return resp;
        }

        /// <summary>
        /// contact Support For Registration
        /// </summary>
        [HttpPost("ContactSupport/{UserType}")]
        public async Task<GenericResponse> ContactSupportForRegistration(ContactSupport Request,string UserType)
        {
            var resp = await _customerServ.ContactSupportForRegistration("", Request,UserType);
            _genServ.LogRequestResponse("ContactSupportForRegistration", JsonConvert.SerializeObject(Request), JsonConvert.SerializeObject(resp));
            return resp;
        }

        [HttpPost("CustomerReasonForNotReceivngOtp/{UserType}")]
        public async Task<GenericResponse> CustomerReasonForNotReceivngOtp(CustomerReasonForNotReceivngOtp Request,string UserType)
        {
            var resp = await _customerServ.CustomerReasonForNotReceivngOtp("", Request,UserType);
            _genServ.LogRequestResponse("CustomerReasonForNotReceivngOtp", JsonConvert.SerializeObject(Request), JsonConvert.SerializeObject(resp));
            return resp;
        }

        /// <summary>
        /// Resend Otp To PhoneNumber
        /// </summary>
        [HttpPost("ResendOtpToPhoneNumber/{UserType}")]
        public async Task<GenericResponse> ResendOtpToPhoneNumber(GenericRegRequest2 Request,string UserType)
        {

            var resp = await _customerServ.ResendOtpToPhoneNumber("", Request,UserType);
            _genServ.LogRequestResponse("ResendOtp", JsonConvert.SerializeObject(Request), JsonConvert.SerializeObject(resp));
            return resp;
        }
    }
}





















