using EntityProject.repositories;
using EntityProject.repositoriesimpl;
using Google.Apis.Auth.OAuth2.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlX.XDevAPI;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Ocsp;
using RestSharp;
using Retailbanking.BL.IServices;
using Retailbanking.BL.Services;
using Retailbanking.Common.CustomObj;
using System.Linq;
using System.Threading.Tasks;
using ClientBankEntity = EntityProject.entities.ClientBank;

namespace assetmanagement.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly IGenericAssetCapitalInsuranceCustomerService _customerServ;
        private readonly ILogger<IGenericAssetCapitalInsuranceCustomerService> _logger;
        private readonly IClientBankRepository clientBankRepository;

        public CustomerController(IGenericAssetCapitalInsuranceCustomerService customerServ, ILogger<IGenericAssetCapitalInsuranceCustomerService> logger, IClientBankRepository clientBankRepository)
        {
            _customerServ = customerServ;
            _logger = logger;
            this.clientBankRepository = clientBankRepository;
        }

        /// <summary>
        ///  Description: The endpoint allows you to save a client KYC item.
        /// </summary>
        [HttpPost("kyc")]
        [Consumes("multipart/form-data")]
        public async Task<GenericResponse2> CreateSimplexKycResponse([FromForm] IFormFile file, [FromForm] SimplexKycForm simplexKycForm, [FromForm] string UserType, [FromForm] string Session, [FromForm] int ChannelId)
        {
            _logger.LogInformation("simplexKycForm " + JsonConvert.SerializeObject(simplexKycForm));
            GenericResponse2 response = await _customerServ.CreateSimplexKycResponse(file, simplexKycForm, UserType, Session, ChannelId);
            _logger.LogInformation("response " + JsonConvert.SerializeObject(response));
            // GenericResponse2 genericResponse2 = JsonConvert.DeserializeObject<GenericResponse2>(response);
            return response;
        }

        /// <summary>
        ///  Description: The endpoint allows you to save client profile picture 
        ///  that is ,upload upload picture.the account is assumed to have been created
        /// </summary>
        [HttpPost("GetClientPicture")]
        [Consumes("multipart/form-data")]
        public async Task<GenericResponse2> GetClientPicture([FromForm] IFormFile file, [FromForm] int ucid, [FromForm] string UserType, [FromForm] string Session, [FromForm] int Channelid)
        {
            _logger.LogInformation("fileName " + file.FileName);
            _logger.LogInformation("ucid " + ucid);
            ClientPictureRequest clientPictureRequest = new ClientPictureRequest();
            clientPictureRequest.ucid = ucid;
            clientPictureRequest.file = file;
            GenericResponse2 response = await _customerServ.GetClientPicture(clientPictureRequest, UserType, Session, Channelid);
            _logger.LogInformation("response " + JsonConvert.SerializeObject(response));
            return response;
        }

        /// <summary>
        ///  Description: The endpoint returns the list to titles from the
        /// </summary>
        [HttpGet("GetClientTitles/{Session}/{UserType}")]
        public async Task<GenericResponse2> GetClientTitles(string Session,string UserType)
        {
            _logger.LogInformation("GetClientTitles " + Session);
            return await _customerServ.GetClientTitles(Session, UserType);
        }


        /// <summary>
        ///  Description: The endpoint returns the list of countries 
        /// </summary>
        [HttpGet("GetCountries/{Session}/{UserType}")]
        public async Task<GenericResponse2> GetCountries(string Session, string UserType)
        {
            _logger.LogInformation("GetCountries " + UserType);
            GenericResponse2 genericResponse2 = await _customerServ.ClientCountries(UserType,Session);
            _logger.LogInformation("genericResponse2 " + JsonConvert.SerializeObject(genericResponse2));
            return genericResponse2;
        }

        /// <summary>
        ///  Description: The endpoint returns the list of states
        /// </summary>
        [HttpGet("GetStates/{Session}/{UserType}")]
        public async Task<GenericResponse2> GetStates(string Session, string UserType)
        {
            _logger.LogInformation("Session " + Session);
            GenericResponse2 genericResponse2 = await _customerServ.ClientStates(Session,UserType);
            _logger.LogInformation("genericResponse2 " + JsonConvert.SerializeObject(genericResponse2));
            return genericResponse2;
        }

        /// <summary>
        ///  Description: The endpoint returns the list of lgs 
        /// </summary>
        [HttpGet("GetLga/{state}/{Session}/{UserType}")]
        public async Task<GenericResponse2> GetLga(string state,string Session,string UserType)
        {
            _logger.LogInformation("GetLga " + state);
            GenericResponse2 genericResponse2 = await _customerServ.ClientLga(state,Session,UserType);
            _logger.LogInformation("genericResponse2 " + JsonConvert.SerializeObject(genericResponse2));
            return genericResponse2;
        }

        /// <summary>
        ///  Description: The endpoint returns the list of employers 
        /// </summary>
        [HttpGet("GetEmployers/{Session}/{UserType}")]
        public async Task<GenericResponse2> GetEmployers(string Session, string UserType)
        {
            _logger.LogInformation("GetEmployers " + Session);
            return await _customerServ.GetEmployers(Session,UserType);
        }

        /// <summary>
        ///  Description: The endpoint returns the list of GetRelationship 
        /// </summary>
        [HttpGet("GetRelationship/{Session}/{UserType}")]
        public async Task<GenericResponse2> GetRelationship(string Session, string UserType)
        {
            _logger.LogInformation("GetRelationship " + Session);
            return await _customerServ.GetRelationship(Session, UserType);
        }

        /// <summary>
        ///  Description:The endpoint allows you fetch client full account details  
        /// </summary>
        [HttpGet("GetFullDetails/{accountCode}/{Session}/{UserType}")]
        public async Task<GenericResponse2> GetFullDetails(int accountCode,string Session,string UserType)
        {
            _logger.LogInformation("GetFullDetails " + Session);
            return await _customerServ.GetFullDetails(accountCode, Session, UserType);
        }


        /// <summary>
        ///  Description:The endpoint allows you fetch client full account details  
        /// </summary>
        [HttpGet("GetFullDetailsByClientReference/{UserName}/{Session}/{UserType}")]
        public async Task<GenericResponse2> GetFullDetailsByClientReference(string UserName, string Session, string UserType)
        {
            _logger.LogInformation("GetFullDetailsByClientReference " + Session);
            GenericResponse2 genericResponse2 =  await _customerServ.GetFullDetailsByClientReference(UserName, Session, UserType);        
            return genericResponse2;
        }

        /// <summary>
        ///  Description: The endpoint returns a list of banks from simplex
        /// </summary>
        [HttpGet("GetInhouseBanks/{UserName}/{Session}/{UserType}")]
        public async Task<GenericResponse2> GetInhouseBanks(string UserName,string Session, string UserType)
        {
            _logger.LogInformation("GetInhouseBanks Session" + Session);
            GenericResponse2 genericResponse2 = await _customerServ.GetInhouseBanks(UserName,Session,UserType);
            _logger.LogInformation("genericResponse2 " + JsonConvert.SerializeObject(genericResponse2));
            return genericResponse2;
        }

        /// <summary>
        ///  Description: The endpoint returns the list of occupations 
        /// </summary>
        [HttpGet("GetOccupations/{UserName}/{Session}/{UserType}")]
        public async Task<GenericResponse2> GetOccupations(string UserName, string Session, string UserType)
        {
            _logger.LogInformation("GetOccupations UserName" + UserName);
            GenericResponse2 genericResponse2 = await _customerServ.GetOccupations(UserName,Session,UserType);
            _logger.LogInformation("genericResponse2 " + JsonConvert.SerializeObject(genericResponse2));
            return genericResponse2;
        }


        /// <summary>
        ///  Description: The endpoint allows you to fetch the various sources of fund.  
        /// </summary>
        [HttpGet("GetSourcesOffund/{Session}/{UserType}")]
        public async Task<GenericResponse2> GetSourcesOffund(string Session,string UserType)
        {
            _logger.LogInformation("GetSourcesOffund ");
            GenericResponse2 genericResponse2 = await _customerServ.GetSourcesOffund(Session,UserType);
            _logger.LogInformation("genericResponse2 " + JsonConvert.SerializeObject(genericResponse2));
            return genericResponse2;
        }

        /// <summary>
        ///  Description: The endpoint returns the list of religions 
        /// </summary>
        [HttpGet("GetReligious/{UserName}/{Session}/{UserType}")]
        public async Task<GenericResponse2> GetReligious(string UserName, string Session, string UserType)
        {
            _logger.LogInformation("GetReligious " + UserName);
            GenericResponse2 genericResponse2 = await _customerServ.GetReligious(UserName, Session,UserType);
            _logger.LogInformation("genericResponse2 " + JsonConvert.SerializeObject(genericResponse2));
            return genericResponse2;
        }

        /// <summary>
        ///  Description: The endpoint to get wallet balance  
        /// </summary>
        [HttpGet("GetWalletBalance/{UserName}/{Session}/{UserType}/{currency}")]
        public async Task<GenericResponse2> GetWalletBalance(string UserName,string Session, string UserType, string currency)
        {
            // wallet/balance/{ currency}/{ clientId}
            _logger.LogInformation("GetWalletBalance " + UserName);
            GenericResponse2 genericResponse2 = await _customerServ.GetWalletBalance(UserName,Session, UserType, currency,0);
            _logger.LogInformation("genericResponse2 " + JsonConvert.SerializeObject(genericResponse2));
            return genericResponse2;
        }


        /// <summary>
        ///  Description: The endpoint allows you to update and existing minor
        ///or add a new minor for a client.The UCID would would 
        ///determine if it is an update or creation. 
        ///If it is 0 it is a create otherwise
        ///and update
        /// </summary>
        [HttpPost("AddOrUpdateClientMinor")]
        public async Task<GenericResponse2> AddOrUpdateClientMinor(SimplexClientMinorDto simplexClientMinor)
        {
            _logger.LogInformation("AddOrUpdateClientMinor " + JsonConvert.SerializeObject(simplexClientMinor));
            GenericResponse2 genericResponse2 = await _customerServ.AddOrUpdateClientMinor(simplexClientMinor);
            _logger.LogInformation("genericResponse2 " + JsonConvert.SerializeObject(genericResponse2));
            return genericResponse2;
        }

        /// <summary>
        ///Description: The endpoint allows you to update and existing Next
        ///Of Kin or add a new Next of Kin for a client.The id would would
        ///determine if it is an update or creation. If it is 0 it is a
        ///create otherwise and update
        /// </summary>
        [HttpPost("AddOrUpdateClientNextOfKin")]
        public async Task<GenericResponse2> AddOrUpdateClientNextOfKin(SimplexClientNextOfKinDto simplexClientNextOfKin)
        {
            _logger.LogInformation("AddOrUpdateClientNextOfKin " + JsonConvert.SerializeObject(simplexClientNextOfKin));
            GenericResponse2 genericResponse2 = await _customerServ.AddOrUpdateClientNextOfKin(simplexClientNextOfKin);
            _logger.LogInformation("genericResponse2 " + JsonConvert.SerializeObject(genericResponse2));
            return genericResponse2;
        }

        /*
        /// <summary>
        ///  Description: The endpoint to get wallet balance  
        /// </summary>
        [HttpGet("GetWalletBalance/{UserName}/{Session}/{UserType}/{currency}")]
        public async Task<GenericResponse2> GetMinorDetails(string UserName, string Session, string UserType, string currency)
        {
            // wallet/balance/{ currency}/{ clientId}
            _logger.LogInformation("GetWalletBalance " + UserName);
            GenericResponse2 genericResponse2 = await _customerServ.GetWalletBalance(UserName, Session, UserType, currency, 0);
            _logger.LogInformation("genericResponse2 " + JsonConvert.SerializeObject(genericResponse2));
            return genericResponse2;
        }
        */

        /// <summary>
        ///  Description: The endpoint to get add or update bank details  
        /// </summary>
        [HttpPost("AddBankOrUpdateForClient")]
        public async Task<GenericResponse2> AddBankOrUpdateForClient(SimplexClientBankDetailsDto simplexClientBankDetails)
        {
            // wallet/balance/{ currency}/{ clientId}
            _logger.LogInformation("AddBankOrUpdateForClient " + JsonConvert.SerializeObject(simplexClientBankDetails));
            GenericResponse2 genericResponse2 = await _customerServ.AddBankOrUpdateForClient(simplexClientBankDetails);
            _logger.LogInformation("genericResponse2 " + JsonConvert.SerializeObject(genericResponse2));
            if(genericResponse2.Success)
            {
               ClientBankEntity clientBankEntity1 =  clientBankRepository.GetClientBankByUserNameAndBankIdAndAccountNumber(simplexClientBankDetails.UserName,
                   simplexClientBankDetails.idBank.ToString(),simplexClientBankDetails.accountNumber,simplexClientBankDetails.UserType);
                if(clientBankEntity1==null)
                {
                    clientBankEntity1 = new ClientBankEntity();
                    clientBankEntity1.AccountNumber = simplexClientBankDetails.accountNumber;
                    clientBankEntity1.AccountName = simplexClientBankDetails.accountName;
                    clientBankEntity1.UserName = simplexClientBankDetails.UserName;
                    clientBankEntity1.BankId = simplexClientBankDetails.idBank.ToString();
                    clientBankEntity1.UserType = simplexClientBankDetails.UserType;
                    bool isClientBankAdded  =clientBankRepository.AddClientBank(clientBankEntity1);
                    _logger.LogInformation("isClientBankAdded " + isClientBankAdded);
                }
            }
            return genericResponse2;
        }

        //bank-details/remove/ucid/idbankaccount
        /// <summary>
        ///  Description: The endpoint to get remove bank for customer 
        /// </summary>
        [HttpPost("RemoveBankOrUpdateForClient/{UserType}")]
        public async Task<GenericResponse2> RemoveBankOrUpdateForClient(SimplexClientBankDetailsRemovalDto simplexClientBankDetailsRemovalDto,string UserType)
        {
            _logger.LogInformation("RemoveBankOrUpdateForClient " + JsonConvert.SerializeObject(simplexClientBankDetailsRemovalDto));
            GenericResponse2 genericResponse2 = await _customerServ.RemoveBankOrUpdateForClient(simplexClientBankDetailsRemovalDto, UserType);
            _logger.LogInformation("genericResponse2 " + JsonConvert.SerializeObject(genericResponse2));
            if(genericResponse2.Success)
            {
               ClientBankEntity clientBankEntity1= clientBankRepository.GetClientBankByUserNameAndBankId(simplexClientBankDetailsRemovalDto.UserName,
                    simplexClientBankDetailsRemovalDto.idBankAccount.ToString(),
                    UserType);
                _logger.LogInformation("clientBankEntity1 " + JsonConvert.SerializeObject(clientBankEntity1));
                if(clientBankEntity1!=null)
                {
                bool remvoalCheck  =clientBankRepository.DeleteClientBankByUserNameAndBankIdAndAccountNumber(simplexClientBankDetailsRemovalDto.UserName,
                    simplexClientBankDetailsRemovalDto.idBankAccount.ToString(),clientBankEntity1.AccountNumber,UserType);
                    _logger.LogInformation("remvoalCheck " + remvoalCheck);
                }
            }
            return genericResponse2;
        }

        /// <summary>
        ///  Description: The endpoint for bank account name enquiry  
        ///  Please note the property-IsAccountOwner.
        ///  if IsAccountOwner is true ,then it is the owner otherwise
        ///  is either the user does not exists or the account does not belong to the 
        ///  user.
        /// </summary>
        [HttpGet("BankAccountNameEnquiry/{UserName}/{Session}/{UserType}/{AccountNumber}/{BankCode}")]
        public async Task<ValidateAccountResponse> BankAccountNameEnquiry(string UserName , string Session, string UserType, string AccountNumber,string BankCode)
        {

            _logger.LogInformation("BankAccountNameEnquiry " + Session);
            ValidateAccountResponse genericResponse2 = await _customerServ.BankAccountNameEnquiry(UserName,Session, UserType,AccountNumber,BankCode);
            _logger.LogInformation("genericResponse2 " + JsonConvert.SerializeObject(genericResponse2));
            return genericResponse2;
        }

        /// <summary>
        ///  Description: The endpoint for bank account name enquiry  
        ///  Please note the property-IsAccountOwner.
        /// </summary>
        [HttpGet("GetAllBanks/{UserName}/{Session}/{UserType}")]
        public async Task<GenericResponse2> GetAllBanks(string UserName, string Session, string UserType)
        {

            _logger.LogInformation("BankAccountNameEnquiry " + Session);
            GenericResponse2 genericResponse2 = await _customerServ.GetAllBanks(UserName, Session, UserType);
            _logger.LogInformation("genericResponse2 " + JsonConvert.SerializeObject(genericResponse2));
            return genericResponse2;
        }

        /// <summary>
        ///  Description: The endpoint for suggested bank  
        /// </summary>
        [HttpGet("GetPossibleBanks/{AccountNumber}/{Session}/{UserType}")]
        public async Task<BankList> GetPossibleBanks(string AccountNumber, string Session, string UserType)
        {

            _logger.LogInformation("GetPossibleBanks " + Session);
            BankList genericResponse2 = await _customerServ.GetPossibleBanks(AccountNumber, Session, UserType);
            _logger.LogInformation("genericResponse2 " + JsonConvert.SerializeObject(genericResponse2));
            return genericResponse2;
        }

    }

}
































































































































































































































































